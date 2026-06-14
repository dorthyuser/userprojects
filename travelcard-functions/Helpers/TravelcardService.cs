using System;
using System.Threading.Tasks;
using Npgsql;
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
            const string travelcardSql = @"INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES (@travelcard_type::travelcard_type_enum, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to)
RETURNING id;";

            await using var cmd = new NpgsqlCommand(travelcardSql, connection, transaction);
            cmd.Parameters.AddWithValue("travelcard_type", request.TravelcardType.ToString());
            cmd.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
            cmd.Parameters.AddWithValue("travelcard_valid_to", request.TravelcardValidTo);
            cmd.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("travelcard_number", request.TravelcardNumber);
            cmd.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
            cmd.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference);
            cmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);

            var travelcardId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            foreach (var cardholder in request.Cardholders)
            {
                const string cardholderSql = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type::cardholder_type_enum, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";

                await using var cardholderCmd = new NpgsqlCommand(cardholderSql, connection, transaction);
                cardholderCmd.Parameters.AddWithValue("travelcard_id", travelcardId);
                cardholderCmd.Parameters.AddWithValue("cardholder_title", cardholder.CardholderTitle);
                cardholderCmd.Parameters.AddWithValue("cardholder_forename", cardholder.CardholderForename);
                cardholderCmd.Parameters.AddWithValue("cardholder_surname", cardholder.CardholderSurname);
                cardholderCmd.Parameters.AddWithValue("cardholder_type", cardholder.CardholderType.ToString());
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_name", cardholder.CardholderPhotoName);
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_rrs_key", (object?)cardholder.CardholderPhotoRRSKey ?? DBNull.Value);
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_url", (object?)cardholder.CardholderPhotoURL ?? DBNull.Value);
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_key", (object?)cardholder.CardholderPhotoKey ?? DBNull.Value);
                await cardholderCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return new CreateTravelcardResponse { TravelcardId = travelcardId.ToString(), Token = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant() };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
