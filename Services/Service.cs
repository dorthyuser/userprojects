using System.Data;
using Amazon.Lambda.Core;
using Npgsql;
using New_project2Lambda.Models;

namespace New_project2Lambda.Services;

public sealed class Service
{
    private readonly NpgsqlDataSource _dataSource;

    public Service(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<Response> CreateAsync(Request request, ILambdaLogger logger)
    {
        logger.LogLine("DB operation: travelcards INSERT");
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var travelcardId = await InsertTravelcardAsync(connection, transaction, request);
        await InsertCardholdersAsync(connection, transaction, travelcardId, request.Cardholders);
        await transaction.CommitAsync();

        logger.LogLine($"DB success: travelcards generated id={travelcardId}");
        return new Response { TravelcardId = travelcardId.ToString(), Token = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant() };
    }

    private static async Task<int> InsertTravelcardAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Request request)
    {
        const string sql = @"INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
                          VALUES (@travelcard_type, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to)
                          RETURNING id;";
        await using var cmd = new NpgsqlCommand(sql, connection, transaction);
        cmd.Parameters.Add(new NpgsqlParameter("travelcard_type", request.TravelcardType));
        cmd.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
        cmd.Parameters.AddWithValue("travelcard_valid_to", request.TravelcardValidTo);
        cmd.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("travelcard_number", request.TravelcardNumber);
        cmd.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
        cmd.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference);
        cmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    private static async Task InsertCardholdersAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, int travelcardId, IEnumerable<CardholderRequest> cardholders)
    {
        const string sql = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
                          VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";
        foreach (var cardholder in cardholders)
        {
            await using var cmd = new NpgsqlCommand(sql, connection, transaction);
            cmd.Parameters.AddWithValue("travelcard_id", travelcardId);
            cmd.Parameters.AddWithValue("cardholder_title", cardholder.CardholderTitle);
            cmd.Parameters.AddWithValue("cardholder_forename", cardholder.CardholderForename);
            cmd.Parameters.AddWithValue("cardholder_surname", cardholder.CardholderSurname);
            cmd.Parameters.Add(new NpgsqlParameter("cardholder_type", cardholder.CardholderType));
            cmd.Parameters.AddWithValue("cardholder_photo_name", cardholder.CardholderPhotoName);
            cmd.Parameters.AddWithValue("cardholder_photo_rrs_key", (object?)cardholder.CardholderPhotoRRSKey ?? DBNull.Value);
            cmd.Parameters.AddWithValue("cardholder_photo_url", (object?)cardholder.CardholderPhotoURL ?? DBNull.Value);
            cmd.Parameters.AddWithValue("cardholder_photo_key", (object?)cardholder.CardholderPhotoKey ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}