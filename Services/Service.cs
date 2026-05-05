using System.Text;
using Amazon.Lambda.Core;
using Npgsql;
using Npgsql.NameTranslation;
using TravelcardchsarplambdaLambda.Models;

namespace TravelcardchsarplambdaLambda.Services;

public class Service
{
    public async Task<CreateResult> CreateAsync(Request request, ILambdaLogger logger)
    {
        logger.LogLine("Creating PostgreSQL connection");
        var dataSource = BuildDataSource();
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var travelcardId = await InsertTravelcardAsync(connection, transaction, request, logger);
            await InsertCardholdersAsync(connection, transaction, travelcardId, request, logger);
            await transaction.CommitAsync();
            return new CreateResult { TravelcardId = travelcardId.ToString(), Token = GenerateToken() };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static NpgsqlDataSource BuildDataSource()
    {
        var conn = new NpgsqlConnectionStringBuilder
        {
            Host = SecretsHelper.Get("host", "POSTGRESQLHOST"),
            Port = int.TryParse(SecretsHelper.Get("port", "POSTGRESQLPORT"), out var port) ? port : 5432,
            Database = SecretsHelper.Get("dbname", "POSTGRESQLDATABASE"),
            Username = SecretsHelper.Get("username", "POSTGRESQLUSERNAME"),
            Password = SecretsHelper.Get("password", "POSTGRESQLPASSWORD")
        };
        var builder = new NpgsqlDataSourceBuilder(conn.ConnectionString);
        builder.MapEnum<TravelcardTypeEnum>("travelcard_type_enum", nameTranslator: new NpgsqlNullNameTranslator());
        builder.MapEnum<CardholderTypeEnum>("cardholder_type_enum", nameTranslator: new NpgsqlNullNameTranslator());
        return builder.Build();
    }

    private static async Task<int> InsertTravelcardAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Request request, ILambdaLogger logger)
    {
        const string sql = @"INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to) VALUES (@travelcard_type, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to) RETURNING id;";
        await using var cmd = new NpgsqlCommand(sql, connection, transaction);
        cmd.Parameters.AddWithValue("travelcard_type", request.TravelcardType);
        cmd.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
        cmd.Parameters.AddWithValue("travelcard_valid_to", request.TravelcardValidTo);
        cmd.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("travelcard_number", request.TravelcardNumber);
        cmd.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
        cmd.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference);
        cmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);
        var id = (int)(await cmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("Failed to create travelcard"));
        logger.LogLine($"Travelcard inserted with id {id}");
        return id;
    }

    private static async Task InsertCardholdersAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, int travelcardId, Request request, ILambdaLogger logger)
    {
        const string sql = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key) VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";
        foreach (var c in request.Cardholders)
        {
            await using var cmd = new NpgsqlCommand(sql, connection, transaction);
            cmd.Parameters.AddWithValue("travelcard_id", travelcardId);
            cmd.Parameters.AddWithValue("cardholder_title", c.CardholderTitle);
            cmd.Parameters.AddWithValue("cardholder_forename", c.CardholderForename);
            cmd.Parameters.AddWithValue("cardholder_surname", c.CardholderSurname);
            cmd.Parameters.AddWithValue("cardholder_type", c.CardholderType);
            cmd.Parameters.AddWithValue("cardholder_photo_name", c.CardholderPhotoName);
            cmd.Parameters.AddWithValue("cardholder_photo_rrs_key", (object?)c.CardholderPhotoRRSKey ?? DBNull.Value);
            cmd.Parameters.AddWithValue("cardholder_photo_url", (object?)c.CardholderPhotoURL ?? DBNull.Value);
            cmd.Parameters.AddWithValue("cardholder_photo_key", (object?)c.CardholderPhotoKey ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
            logger.LogLine($"Inserted cardholder {c.CardholderType}");
        }
    }

    private static string GenerateToken()
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString("N")))[..6].ToUpperInvariant();
    }
}