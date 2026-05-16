using Amazon.Lambda.Core;
using Npgsql;
using DemoTravelcardsLambda.Models;

namespace DemoTravelcardsLambda.Services;

public class Service
{
    private readonly NpgsqlDataSource _dataSource;

    public Service(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<ResponseDto> CreateAsync(RequestDto request, ILambdaContext context)
    {
        context.Logger.LogLine("DB operation: INSERT on travelcards and cardholders");
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();

        var travelcardId = await InsertTravelcardAsync(conn, tx, request);
        foreach (var cardholder in request.Cardholders)
        {
            await InsertCardholderAsync(conn, tx, travelcardId, cardholder);
        }
        await tx.CommitAsync();
        return new ResponseDto { TravelcardId = travelcardId.ToString(), Token = GenerateToken() };
    }

    private static async Task<int> InsertTravelcardAsync(NpgsqlConnection conn, NpgsqlTransaction tx, RequestDto request)
    {
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO public.travelcards (
                travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name,
                travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
            VALUES (
                @travelcard_type, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name,
                @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to)
            RETURNING id;", conn, tx);
        cmd.Parameters.AddWithValue("travelcard_type", request.TravelcardType);
        cmd.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
        cmd.Parameters.AddWithValue("travelcard_valid_to", request.TravelcardValidTo);
        cmd.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("travelcard_number", request.TravelcardNumber);
        cmd.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
        cmd.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference);
        cmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    private static async Task InsertCardholderAsync(NpgsqlConnection conn, NpgsqlTransaction tx, int travelcardId, CardholderRequestDto cardholder)
    {
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO public.cardholders (
                travelcard_id, cardholder_title, cardholder_forename, cardholder_surname,
                cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
            VALUES (
                @travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname,
                @cardholder_type, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);", conn, tx);
        cmd.Parameters.AddWithValue("travelcard_id", travelcardId);
        cmd.Parameters.AddWithValue("cardholder_title", cardholder.CardholderTitle);
        cmd.Parameters.AddWithValue("cardholder_forename", cardholder.CardholderForename);
        cmd.Parameters.AddWithValue("cardholder_surname", cardholder.CardholderSurname);
        cmd.Parameters.AddWithValue("cardholder_type", cardholder.CardholderType);
        cmd.Parameters.AddWithValue("cardholder_photo_name", cardholder.CardholderPhotoName);
        cmd.Parameters.AddWithValue("cardholder_photo_rrs_key", (object?)cardholder.CardholderPhotoRRSKey ?? DBNull.Value);
        cmd.Parameters.AddWithValue("cardholder_photo_url", (object?)cardholder.CardholderPhotoURL ?? DBNull.Value);
        cmd.Parameters.AddWithValue("cardholder_photo_key", (object?)cardholder.CardholderPhotoKey ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    private static string GenerateToken() => Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", string.Empty).Replace("+", string.Empty).Replace("/", string.Empty).Substring(0, 6);
}