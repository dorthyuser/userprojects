using System.Text.Json;
using Npgsql;
using LambdatestingtravelcardLambda.Models;

namespace LambdatestingtravelcardLambda.Services;

public sealed class Service
{
    private readonly NpgsqlDataSource _dataSource;

    public Service(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<Response> CreateAsync(Request request)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();

        long travelcardId;
        const string travelcardSql = @"INSERT INTO public.travelcards
(travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES (@travelcard_type, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to)
RETURNING id;";

        await using (var cmd = new NpgsqlCommand(travelcardSql, conn, tx))
        {
            cmd.Parameters.Add(new NpgsqlParameter("travelcard_type", request.TravelcardType));
            cmd.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
            cmd.Parameters.AddWithValue("travelcard_valid_to", request.TravelcardValidTo);
            cmd.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("travelcard_number", request.TravelcardNumber);
            cmd.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
            cmd.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference);
            cmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);
            travelcardId = Convert.ToInt64(await cmd.ExecuteScalarAsync());
        }

        const string cardholderSql = @"INSERT INTO public.cardholders
(travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";

        foreach (var ch in request.Cardholders)
        {
            await using var cmd = new NpgsqlCommand(cardholderSql, conn, tx);
            cmd.Parameters.AddWithValue("travelcard_id", (int)travelcardId);
            cmd.Parameters.AddWithValue("cardholder_title", ch.CardholderTitle);
            cmd.Parameters.AddWithValue("cardholder_forename", ch.CardholderForename);
            cmd.Parameters.AddWithValue("cardholder_surname", ch.CardholderSurname);
            cmd.Parameters.Add(new NpgsqlParameter("cardholder_type", ch.CardholderType));
            cmd.Parameters.AddWithValue("cardholder_photo_name", ch.CardholderPhotoName);
            cmd.Parameters.AddWithValue("cardholder_photo_rrs_key", (object?)ch.CardholderPhotoRRSKey ?? DBNull.Value);
            cmd.Parameters.AddWithValue("cardholder_photo_url", (object?)ch.CardholderPhotoURL ?? DBNull.Value);
            cmd.Parameters.AddWithValue("cardholder_photo_key", (object?)ch.CardholderPhotoKey ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();

        return new Response
        {
            TravelcardId = Guid.NewGuid().ToString(),
            Token = Random.Shared.Next(100000, 999999).ToString()
        };
    }
}