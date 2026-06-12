using System;
using System.Threading.Tasks;
using Npgsql;
using TravelCardFunctionApp.Helpers;
using TravelCardFunctionApp.Models;

namespace TravelCardFunctionApp.Services;

public class TravelCardService
{
    private readonly DbHelper _dbHelper;

    public TravelCardService(DbHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }

    public async Task<CreateTravelCardResponse> CreateAsync(CreateTravelCardRequest request, string clientId, string? correlationId)
    {
        await using var connection = await _dbHelper.DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO public.travelcards (
                travelcard_type,
                travelcard_valid_from,
                travelcard_valid_to,
                travelcard_name,
                travelcard_number,
                travelcard_requested_date,
                travelcard_transaction_reference,
                travelcard_usable_to
            )
            VALUES (
                @travelcard_type::travelcard_type_enum,
                @travelcard_valid_from,
                @travelcard_valid_to,
                @travelcard_name,
                @travelcard_number,
                @travelcard_requested_date,
                @travelcard_transaction_reference,
                @travelcard_usable_to
            )
            RETURNING id;", connection, transaction);

        cmd.Parameters.AddWithValue("travelcard_type", request.TravelcardType.ToString());
        cmd.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
        cmd.Parameters.AddWithValue("travelcard_valid_to", request.TravelcardValidTo);
        cmd.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("travelcard_number", request.TravelcardNumber);
        cmd.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
        cmd.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference);
        cmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);

        var travelcardId = (int)(await cmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("Failed to create travelcard."));

        foreach (var cardholder in request.Cardholders)
        {
            await using var cardholderCmd = new NpgsqlCommand(@"
                INSERT INTO public.cardholders (
                    travelcard_id,
                    cardholder_title,
                    cardholder_forename,
                    cardholder_surname,
                    cardholder_type,
                    cardholder_photo_name,
                    cardholder_photo_rrs_key,
                    cardholder_photo_url,
                    cardholder_photo_key
                )
                VALUES (
                    @travelcard_id,
                    @cardholder_title,
                    @cardholder_forename,
                    @cardholder_surname,
                    @cardholder_type::cardholder_type_enum,
                    @cardholder_photo_name,
                    @cardholder_photo_rrs_key,
                    @cardholder_photo_url,
                    @cardholder_photo_key
                );", connection, transaction);

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
        return new CreateTravelCardResponse
        {
            TravelcardId = Guid.NewGuid().ToString(),
            Token = "P5SSY6"
        };
    }
}
