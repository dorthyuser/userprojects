using System.Text;
using Amazon.Lambda.Core;
using Npgsql;
using Npgsql.NameTranslation;
using Travelcardcharplambda225Lambda.Models;

namespace Travelcardcharplambda225Lambda.Services;

public sealed class Service
{
    private static readonly Lazy<NpgsqlDataSource> DataSource = new(CreateDataSource);

    public async Task<Response> CreateTravelcardAsync(Request request, ILambdaLogger logger)
    {
        logger.LogLine("DB operation: travelcards INSERT");
        await using var connection = await DataSource.Value.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        const string travelcardSql = @"
INSERT INTO public.travelcards
(travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES
(@travelcard_type, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to)
RETURNING id;";

        await using var cmd = new NpgsqlCommand(travelcardSql, connection, transaction);
        cmd.Parameters.Add(new NpgsqlParameter("travelcard_type", request.TravelcardType));
        cmd.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
        cmd.Parameters.AddWithValue("travelcard_valid_to", request.TravelcardValidTo);
        cmd.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("travelcard_number", request.TravelcardNumber);
        cmd.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
        cmd.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference);
        cmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);

        var travelcardId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        logger.LogLine($"DB success: travelcards generated ID={travelcardId}");

        foreach (var cardholder in request.Cardholders)
        {
            logger.LogLine("DB operation: cardholders INSERT");
            const string cardholderSql = @"
INSERT INTO public.cardholders
(travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES
(@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";
            await using var cCmd = new NpgsqlCommand(cardholderSql, connection, transaction);
            cCmd.Parameters.AddWithValue("travelcard_id", travelcardId);
            cCmd.Parameters.AddWithValue("cardholder_title", cardholder.CardholderTitle);
            cCmd.Parameters.AddWithValue("cardholder_forename", cardholder.CardholderForename);
            cCmd.Parameters.AddWithValue("cardholder_surname", cardholder.CardholderSurname);
            cCmd.Parameters.Add(new NpgsqlParameter("cardholder_type", cardholder.CardholderType));
            cCmd.Parameters.AddWithValue("cardholder_photo_name", cardholder.CardholderPhotoName);
            cCmd.Parameters.AddWithValue("cardholder_photo_rrs_key", (object?)cardholder.CardholderPhotoRRSKey ?? DBNull.Value);
            cCmd.Parameters.AddWithValue("cardholder_photo_url", (object?)cardholder.CardholderPhotoURL ?? DBNull.Value);
            cCmd.Parameters.AddWithValue("cardholder_photo_key", (object?)cardholder.CardholderPhotoKey ?? DBNull.Value);
            await cCmd.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
        logger.LogLine($"DB success: travelcards generated ID={travelcardId}");

        return new Response
        {
            TravelcardId = travelcardId.ToString(),
            Token = GenerateToken(travelcardId)
        };
    }

    private static string GenerateToken(int id)
    {
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"TC{id}"));
        return token.Replace("=", string.Empty).Replace("+", string.Empty).Replace("/", string.Empty).Substring(0, Math.Min(6, token.Length));
    }

    private static NpgsqlDataSource CreateDataSource()
    {
        var cs = BuildConnectionString();
        var builder = new NpgsqlDataSourceBuilder(cs);
        builder.MapEnum<TravelcardType>("travelcard_type_enum", new NpgsqlNullNameTranslator());
        builder.MapEnum<CardholderType>("cardholder_type_enum", new NpgsqlNullNameTranslator());
        return builder.Build();
    }

    private static string BuildConnectionString()
    {
        var host = SecretsHelper.Get("host", "POSTGRESQLHOST");
        var port = SecretsHelper.Get("port", "POSTGRESQLPORT");
        var db = SecretsHelper.Get("dbname", "POSTGRESQLDATABASE");
        var user = SecretsHelper.Get("username", "POSTGRESQLUSERNAME");
        var password = SecretsHelper.Get("password", "POSTGRESQLPASSWORD");
        return $"Host={host};Port={port};Database={db};Username={user};Password={password};Pooling=true;Include Error Detail=false";
    }
}