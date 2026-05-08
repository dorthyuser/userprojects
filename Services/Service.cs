using Amazon.Lambda.Core;
using Npgsql;
using Travelcardlambdacsharp1105Lambda.Models;

namespace Travelcardlambdacsharp1105Lambda.Services;

public sealed class Service
{
    private readonly ILambdaLogger _logger;
    public Service(ILambdaLogger logger) => _logger = logger;

    public async Task<CreateTravelcardResponse> CreateAsync(CreateTravelcardRequest request, IDictionary<string, string>? headers, IDictionary<string, string>? query, IDictionary<string, string>? route)
    {
        _logger.LogLine("DB operation: travelcards INSERT");
        await using var dataSource = NpgsqlDataSource.Create(BuildConnectionString());
        await using var conn = await dataSource.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();

        await using var cmd = new NpgsqlCommand(@"
INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES (@travelcard_type, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to)
RETURNING id;", conn, tx);
        cmd.Parameters.Add(new NpgsqlParameter("travelcard_type", request.TravelcardType));
        cmd.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
        cmd.Parameters.AddWithValue("travelcard_valid_to", request.TravelcardValidTo);
        cmd.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("travelcard_number", request.TravelcardNumber);
        cmd.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
        cmd.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference);
        cmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);
        var travelcardId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        foreach (var cardholder in request.Cardholders)
        {
            await using var chCmd = new NpgsqlCommand(@"
INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);", conn, tx);
            chCmd.Parameters.AddWithValue("travelcard_id", travelcardId);
            chCmd.Parameters.AddWithValue("cardholder_title", cardholder.CardholderTitle);
            chCmd.Parameters.AddWithValue("cardholder_forename", cardholder.CardholderForename);
            chCmd.Parameters.AddWithValue("cardholder_surname", cardholder.CardholderSurname);
            chCmd.Parameters.Add(new NpgsqlParameter("cardholder_type", cardholder.CardholderType));
            chCmd.Parameters.AddWithValue("cardholder_photo_name", cardholder.CardholderPhotoName);
            chCmd.Parameters.AddWithValue("cardholder_photo_rrs_key", (object?)cardholder.CardholderPhotoRRSKey ?? DBNull.Value);
            chCmd.Parameters.AddWithValue("cardholder_photo_url", (object?)cardholder.CardholderPhotoURL ?? DBNull.Value);
            chCmd.Parameters.AddWithValue("cardholder_photo_key", (object?)cardholder.CardholderPhotoKey ?? DBNull.Value);
            await chCmd.ExecuteNonQueryAsync();
        }
        await tx.CommitAsync();
        return new CreateTravelcardResponse { TravelcardId = travelcardId.ToString(), Token = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant() };
    }

    private static string BuildConnectionString()
    {
        var host = SecretsHelper.Get("host", "POSTGRESQLHOST");
        var port = SecretsHelper.Get("port", "POSTGRESQLPORT");
        var db = SecretsHelper.Get("dbname", "POSTGRESQLDATABASE");
        var user = SecretsHelper.Get("username", "POSTGRESQLUSERNAME");
        var pass = SecretsHelper.Get("password", "POSTGRESQLPASSWORD");
        return $"Host={host};Port={port};Database={db};Username={user};Password={pass};Pooling=true;Include Error Detail=true";
    }
}