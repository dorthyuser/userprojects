using System.Net;
using DemoTravelcardClincalLambda.Enums;
using DemoTravelcardClincalLambda.Models;
using Microsoft.Extensions.Logging;
using Npgsql;
using Npgsql.NameTranslation;

namespace DemoTravelcardClincalLambda.Services;

public sealed class Service
{
    private readonly ILogger _logger;
    private readonly NpgsqlDataSource _dataSource;

    public Service(ILogger logger)
    {
        _logger = logger;
        var builder = new NpgsqlDataSourceBuilder(BuildConnectionString());
        builder.MapEnum<TravelcardType>("travelcard_type_enum", new NpgsqlNullNameTranslator());
        builder.MapEnum<CardholderType>("cardholder_type_enum", new NpgsqlNullNameTranslator());
        _dataSource = builder.Build();
    }

    public async Task<Response> CreateTravelcardAsync(Request request)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();

        const string insertTravelcardSql = @"INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES (@travelcard_type, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to)
RETURNING id;";

        await using var cmd = new NpgsqlCommand(insertTravelcardSql, conn, tx);
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
            const string insertCardholderSql = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key)
RETURNING id;";

            await using var cardCmd = new NpgsqlCommand(insertCardholderSql, conn, tx);
            cardCmd.Parameters.AddWithValue("travelcard_id", travelcardId);
            cardCmd.Parameters.AddWithValue("cardholder_title", cardholder.CardholderTitle);
            cardCmd.Parameters.AddWithValue("cardholder_forename", cardholder.CardholderForename);
            cardCmd.Parameters.AddWithValue("cardholder_surname", cardholder.CardholderSurname);
            cardCmd.Parameters.Add(new NpgsqlParameter("cardholder_type", cardholder.CardholderType));
            cardCmd.Parameters.AddWithValue("cardholder_photo_name", cardholder.CardholderPhotoName);
            cardCmd.Parameters.AddWithValue("cardholder_photo_rrs_key", (object?)cardholder.CardholderPhotoRRSKey ?? DBNull.Value);
            cardCmd.Parameters.AddWithValue("cardholder_photo_url", (object?)cardholder.CardholderPhotoURL ?? DBNull.Value);
            cardCmd.Parameters.AddWithValue("cardholder_photo_key", (object?)cardholder.CardholderPhotoKey ?? DBNull.Value);
            await cardCmd.ExecuteScalarAsync();
        }

        await tx.CommitAsync();
        return new Response { TravelcardId = Guid.NewGuid().ToString(), Token = GenerateToken() };
    }

    private static string BuildConnectionString()
    {
        var host = SecretsHelper.Get("host", "POSTGRESQLHOST");
        var port = SecretsHelper.Get("port", "POSTGRESQLPORT");
        var dbname = SecretsHelper.Get("dbname", "POSTGRESQLDATABASE");
        var username = SecretsHelper.Get("username", "POSTGRESQLUSERNAME");
        var password = SecretsHelper.Get("password", "POSTGRESQLPASSWORD");
        return $"Host={host};Port={port};Database={dbname};Username={username};Password={password};Pooling=true;Minimum Pool Size=0;Maximum Pool Size=50;Timeout=15;Command Timeout=30";
    }

    private static string GenerateToken() => Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", string.Empty).Replace("+", string.Empty).Replace("/", string.Empty)[..6].ToUpperInvariant();
}

public static class RequestValidator
{
    public static string? Validate(Request request)
    {
        var now = DateTimeOffset.UtcNow;
        if (request.TravelcardRequestedDate > now) return "travelcardRequestedDate must be in the past.";
        if (request.TravelcardValidFrom > request.TravelcardValidTo) return "travelcardValidFrom must not be later than travelcardValidTo.";
        if (request.TravelcardValidTo <= now) return "travelcardValidTo must be in the future.";
        if (request.TravelcardValidFrom > now.AddMonths(1)) return "travelcardValidFrom must be no later than one calendar month from today.";
        if (request.TravelcardType == TravelcardType.SixteenToSeventeen && request.TravelcardUsableTo is null) return "travelcardUsableTo is required for SixteenToSeventeen travelcards.";
        if (request.TravelcardUsableTo is not null && request.TravelcardUsableTo <= now) return "travelcardUsableTo must be in the future.";
        if ((request.TravelcardType == TravelcardType.SixteenToSeventeen || request.TravelcardType == TravelcardType.Veterans) && request.Cardholders.Any(c => c.CardholderType == CardholderType.Secondary)) return "Secondary cardholder is not allowed for this travelcard type.";
        if (request.Cardholders.Count is < 1 or > 2) return "cardholders must contain exactly one or two items.";
        if (request.Cardholders.Count(c => c.CardholderType == CardholderType.Primary) != 1) return "Exactly one Primary cardholder is required.";
        foreach (var c in request.Cardholders)
        {
            var count = new[] { c.CardholderPhotoRRSKey, c.CardholderPhotoURL, c.CardholderPhotoKey }.Count(x => !string.IsNullOrWhiteSpace(x));
            if (count != 1) return "Each cardholder must provide exactly one photo identifier.";
        }
        return null;
    }
}