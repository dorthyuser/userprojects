using System.Text.Json;
using Amazon.Lambda.Core;
using Npgsql;
using Travelcarddemo1251Lambda.Models;

namespace Travelcarddemo1251Lambda.Services;

public class Service
{
    private readonly NpgsqlDataSource _dataSource;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Service(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<TravelcardResponse> CreateTravelcardAsync(CreateTravelcardRequest request, ILambdaContext context)
    {
        context.Logger.LogLine("DB operation: travelcards INSERT");
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var travelcardId = await InsertTravelcardAsync(connection, transaction, request, context);
            await InsertCardholdersAsync(connection, transaction, travelcardId, request, context);
            await transaction.CommitAsync();
            context.Logger.LogLine($"DB success: travelcards generated ID {travelcardId}");
            return new TravelcardResponse { TravelcardId = travelcardId.ToString(), Token = GenerateToken() };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static string GenerateToken() => Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

    private static async Task<int> InsertTravelcardAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CreateTravelcardRequest request, ILambdaContext context)
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
        return (int)(await cmd.ExecuteScalarAsync() ?? 0);
    }

    private static async Task InsertCardholdersAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, int travelcardId, CreateTravelcardRequest request, ILambdaContext context)
    {
        context.Logger.LogLine("DB operation: cardholders INSERT");
        foreach (var cardholder in request.Cardholders)
        {
            const string sql = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";
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