using System.Globalization;
using Npgsql;
using Npgsql.NameTranslation;
using Travelcardcharplambda1031Lambda.Models;

namespace Travelcardcharplambda1031Lambda.Services;

public sealed class Service
{
    public async Task<CreateTravelcardResponse> CreateTravelcardAsync(CreateTravelcardRequest request, Amazon.Lambda.Core.ILambdaContext context)
    {
        var connectionString = BuildConnectionString();
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.MapEnum<TravelcardType>("travelcard_type_enum", new NpgsqlNullNameTranslator());
        dataSourceBuilder.MapEnum<CardholderType>("cardholder_type_enum", new NpgsqlNullNameTranslator());
        await using var dataSource = dataSourceBuilder.Build();
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        context.Logger.LogLine("DB operation: travelcards INSERT");
        var travelcardId = await InsertTravelcardAsync(connection, transaction, request);
        context.Logger.LogLine($"DB success: travelcards generated id={travelcardId}");

        context.Logger.LogLine("DB operation: cardholders INSERT");
        await InsertCardholdersAsync(connection, transaction, request, travelcardId);
        context.Logger.LogLine($"DB success: cardholders generated id={travelcardId}");

        await transaction.CommitAsync();

        return new CreateTravelcardResponse
        {
            TravelcardId = travelcardId.ToString(CultureInfo.InvariantCulture),
            Token = GenerateToken()
        };
    }

    private static string BuildConnectionString()
    {
        var host = SecretsHelper.Get("host", "POSTGRESQLHOST");
        var port = SecretsHelper.Get("port", "POSTGRESQLPORT");
        var dbName = SecretsHelper.Get("dbname", "POSTGRESQLDATABASE");
        var userName = SecretsHelper.Get("username", "POSTGRESQLUSERNAME");
        var password = SecretsHelper.Get("password", "POSTGRESQLPASSWORD");

        return $"Host={host};Port={port};Database={dbName};Username={userName};Password={password};Pooling=true;SSL Mode=Require;Trust Server Certificate=true";
    }

    private static async Task<int> InsertTravelcardAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CreateTravelcardRequest request)
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
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private static async Task InsertCardholdersAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CreateTravelcardRequest request, int travelcardId)
    {
        const string sql = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";

        foreach (var cardholder in request.Cardholders)
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

    private static string GenerateToken() => Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
}