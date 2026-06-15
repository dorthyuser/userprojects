using System;
using System.Threading.Tasks;
using Npgsql;
using travelcard_function_app.Helpers;
using travelcard_function_app.Models;

namespace travelcard_function_app.Services;

public class TravelcardRepository
{
    private readonly DbHelper _dbHelper;

    public TravelcardRepository(DbHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }

    public async Task<(int TravelcardId, string Token)> InsertAsync(CreateTravelcardRequest request)
    {
        await using var connection = await _dbHelper.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            const string travelcardSql = @"
INSERT INTO public.travelcards
(travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES
(@travelcard_type::travelcard_type_enum, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to)
RETURNING id;";

            await using var travelcardCmd = new NpgsqlCommand(travelcardSql, connection, transaction);
            travelcardCmd.Parameters.AddWithValue("travelcard_type", request.TravelcardType.ToString());
            travelcardCmd.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
            travelcardCmd.Parameters.AddWithValue("travelcard_valid_to", request.TravelcardValidTo);
            travelcardCmd.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);
            travelcardCmd.Parameters.AddWithValue("travelcard_number", request.TravelcardNumber);
            travelcardCmd.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
            travelcardCmd.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference);
            travelcardCmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);

            var travelcardId = Convert.ToInt32(await travelcardCmd.ExecuteScalarAsync());
            foreach (var cardholder in request.Cardholders)
            {
                const string cardholderSql = @"
INSERT INTO public.cardholders
(travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES
(@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type::cardholder_type_enum, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";

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
            return (travelcardId, GenerateToken());
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static string GenerateToken()
    {
        return Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
    }
}
