using Npgsql;
using Npgsql.NameTranslation;
using Travelcardcsharplambda355Lambda.Enums;
using Travelcardcsharplambda355Lambda.Models;

namespace Travelcardcsharplambda355Lambda.Services;

public sealed class Service
{
    public async Task<Response> CreateTravelcardAsync(Request request)
    {
        var builder = new NpgsqlDataSourceBuilder(BuildConnectionString());
        builder.MapEnum<TravelcardType>("travelcard_type_enum", new NpgsqlSnakeCaseNameTranslator());
        builder.MapEnum<CardholderType>("cardholder_type_enum", new NpgsqlSnakeCaseNameTranslator());

        await using var dataSource = builder.Build();
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var travelcardId = await InsertTravelcardAsync(connection, transaction, request);
            await InsertCardholdersAsync(connection, transaction, travelcardId, request);
            await transaction.CommitAsync();
            return new Response
            {
                TravelcardId = Guid.NewGuid().ToString(),
                Token = "P5SSY6"
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static string BuildConnectionString()
    {
        var host = SecretsHelper.Get("host", "POSTGRESQLHOST");
        var port = SecretsHelper.Get("port", "POSTGRESQLPORT");
        var database = SecretsHelper.Get("dbname", "POSTGRESQLDATABASE");
        var username = SecretsHelper.Get("username", "POSTGRESQLUSERNAME");
        var password = SecretsHelper.Get("password", "POSTGRESQLPASSWORD");
        return $"Host={host};Port={port};Database={database};Username={username};Password={password};Pooling=true;";
    }

    private static async Task<int> InsertTravelcardAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Request request)
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
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private static async Task InsertCardholdersAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, int travelcardId, Request request)
    {
        const string sql = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key) VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";
        foreach (var cardholder in request.Cardholders)
        {
            await using var cmd = new NpgsqlCommand(sql, connection, transaction);
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
    }
}
