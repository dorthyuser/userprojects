using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using travelcard_functions.Models;

namespace travelcard_functions.Helpers;

public class TravelcardService
{
    private readonly DbHelper _dbHelper;

    public TravelcardService(DbHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }

    public async Task<CreateTravelcardResponse> CreateAsync(CreateTravelcardRequest request)
    {
        await using var connection = await _dbHelper.DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // Ensure travelcard_number matches DB expectations by removing separators and normalising case
            var travelcardNumberClean = Regex.Replace(request.TravelcardNumber ?? string.Empty, "[^A-Za-z0-9]", "").ToUpperInvariant();
            var travelcardNumberOriginal = (request.TravelcardNumber ?? string.Empty).Trim();

            // Pre-validate against expected database check constraint to avoid DB-level 23514 errors.
            // The DB constraint enforces a strict alphanumeric format and length; map that to a friendly validation error.
            // Accept exactly 11 alphanumeric characters to match the DB check constraint.
            if (!Regex.IsMatch(travelcardNumberClean, "^[A-Z0-9]{11}$"))
            {
                throw new ArgumentException("Validation Error: travelcardNumber is invalid.");
            }

            // Use explicit CAST instead of Postgres '::' inline cast to avoid parameter parsing ambiguities
            const string travelcardSql = @"INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES (CAST(@travelcard_type AS travelcard_type_enum), @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to)
RETURNING id;";

            // Use the validated cleaned travelcard number for insertion so it satisfies the DB check constraint
            int travelcardId;
            try
            {
                travelcardId = await InsertTravelcard(connection, transaction, travelcardSql, request, travelcardNumberClean);
            }
            catch (PostgresException pex) when (pex.SqlState == "23514" && string.Equals(pex.ConstraintName, "travelcards_travelcard_number_check", StringComparison.OrdinalIgnoreCase))
            {
                await transaction.RollbackAsync();
                throw new ArgumentException("Validation Error: travelcardNumber is invalid.", pex);
            }

            foreach (var cardholder in request.Cardholders)
            {
                // Use CAST for enum to avoid parameter parsing issues
                const string cardholderSql = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, CAST(@cardholder_type AS cardholder_type_enum), @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";

                await using var cardholderCmd = new NpgsqlCommand(cardholderSql, connection, transaction);
                cardholderCmd.Parameters.AddWithValue("travelcard_id", travelcardId);
                cardholderCmd.Parameters.AddWithValue("cardholder_title", cardholder.CardholderTitle);
                cardholderCmd.Parameters.AddWithValue("cardholder_forename", cardholder.CardholderForename);
                cardholderCmd.Parameters.AddWithValue("cardholder_surname", cardholder.CardholderSurname);
                // Explicitly type the cardholder_type parameter as text so CAST works reliably
                cardholderCmd.Parameters.Add(new NpgsqlParameter("cardholder_type", NpgsqlDbType.Varchar) { Value = cardholder.CardholderType.ToString() });
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_name", cardholder.CardholderPhotoName);
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_rrs_key", string.IsNullOrWhiteSpace(cardholder.CardholderPhotoRRSKey) ? DBNull.Value : (object)cardholder.CardholderPhotoRRSKey);
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_url", string.IsNullOrWhiteSpace(cardholder.CardholderPhotoURL) ? DBNull.Value : (object)cardholder.CardholderPhotoURL);
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_key", string.IsNullOrWhiteSpace(cardholder.CardholderPhotoKey) ? DBNull.Value : (object)cardholder.CardholderPhotoKey);
                await cardholderCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return new CreateTravelcardResponse { TravelcardId = travelcardId.ToString(), Token = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant() };
        }
        catch (PostgresException pex)
        {
            // Rollback and translate DB constraint violations into a friendly validation error
            await transaction.RollbackAsync();
            if (pex.SqlState == "23514")
            {
                // Constraint violation (check constraint) - map to validation error
                var constraint = string.IsNullOrWhiteSpace(pex.ConstraintName) ? "travelcard data" : pex.ConstraintName;
                // Provide a clear message to the caller that the travelcard number failed DB validation
                if (string.Equals(constraint, "travelcards_travelcard_number_check", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Validation Error: travelcardNumber is invalid.", pex);
                }

                throw new ArgumentException($"Validation Error: database constraint '{constraint}' was violated.", pex);
            }

            throw;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task<int> InsertTravelcard(NpgsqlConnection connection, NpgsqlTransaction transaction, string travelcardSql, CreateTravelcardRequest request, string travelcardNumberValue)
    {
        await using var cmd = new NpgsqlCommand(travelcardSql, connection, transaction);
        // Provide explicit types for a couple of parameters to make intent clear and avoid driver misinterpretation
        var pType = new NpgsqlParameter("travelcard_type", NpgsqlDbType.Varchar) { Value = request.TravelcardType.ToString() };
        cmd.Parameters.Add(pType);
        cmd.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
        cmd.Parameters.AddWithValue("travelcard_valid_to", request.TravelcardValidTo);
        cmd.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);

        // Ensure travelcard_number is provided as text and trimmed — use the cleaned value validated above
        var pNumber = new NpgsqlParameter("travelcard_number", NpgsqlDbType.Varchar) { Value = travelcardNumberValue };
        // Explicitly set the declared Size so the parameter is transmitted with the expected length metadata.
        // This can help the server-side check constraint evaluation behave consistently.
        pNumber.Size = travelcardNumberValue?.Length ?? 0;
        cmd.Parameters.Add(pNumber);

        cmd.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
        cmd.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference);
        cmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);

        var travelcardIdObj = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(travelcardIdObj);
    }
}
