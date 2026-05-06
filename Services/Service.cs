using System.Data;
using Npgsql;
using Npgsql.NameTranslation;
using Microsoft.Extensions.Logging;
using TestCsharpLambTc20260506Lambda.Enums;
using TestCsharpLambTc20260506Lambda.Models;

namespace TestCsharpLambTc20260506Lambda.Services;

public sealed class Service
{
    private readonly ILogger<Service> _logger;

    public Service(ILogger<Service> logger)
    {
        _logger = logger;
    }

    public ValidationResult ValidateRequest(Request request)
    {
        _logger.LogInformation("Validating request");
        var now = DateTime.UtcNow;

        if (!Enum.IsDefined(typeof(TravelcardTypeEnum), request.TravelcardType)) return Fail("Invalid travelcardType");
        if (request.TravelcardRequestedDate >= now) return Fail("travelcardRequestedDate must be in the past");
        if (request.TravelcardValidTo <= now) return Fail("travelcardValidTo must be in the future");
        if (request.TravelcardValidFrom > request.TravelcardValidTo) return Fail("travelcardValidFrom cannot be later than travelcardValidTo");
        if (request.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen && request.TravelcardUsableTo is null) return Fail("travelcardUsableTo is required for SixteenToSeventeen");
        if (request.TravelcardUsableTo is not null && request.TravelcardUsableTo <= now) return Fail("travelcardUsableTo must be in the future");
        if (request.Cardholders is null || request.Cardholders.Count is < 1 or > 2) return Fail("cardholders must contain exactly one primary and optionally one secondary");
        if (request.Cardholders.Count(c => c.CardholderType == CardholderTypeEnum.Primary) != 1) return Fail("Exactly one Primary cardholder is required");
        if (request.Cardholders.Count(c => c.CardholderType == CardholderTypeEnum.Secondary) > 1) return Fail("Only one Secondary cardholder is allowed");
        if ((request.TravelcardType is TravelcardTypeEnum.Young or TravelcardTypeEnum.SixteenToSeventeen or TravelcardTypeEnum.TwentySixToThirty or TravelcardTypeEnum.Veterans or TravelcardTypeEnum.Senior) && request.Cardholders.Any(c => c.CardholderType == CardholderTypeEnum.Secondary)) return Fail("Secondary cardholder not allowed for this travelcard type");
        return ValidationResult.Ok();
    }

    public async Task<Response> CreateTravelcardAsync(Request request)
    {
        _logger.LogInformation("Creating travelcard and cardholders");
        var connString = BuildConnectionString();
        var builder = new NpgsqlDataSourceBuilder(connString);
        builder.MapEnum<TravelcardTypeEnum>("travelcard_type_enum", new NpgsqlNullNameTranslator());
        builder.MapEnum<CardholderTypeEnum>("cardholder_type_enum", new NpgsqlNullNameTranslator());
        await using var dataSource = builder.Build();
        await using var conn = await dataSource.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            await using var cmd = new NpgsqlCommand(@"INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to) VALUES (@travelcard_type, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to) RETURNING id;", conn, tx);
            cmd.Parameters.Add(new NpgsqlParameter("travelcard_type", request.TravelcardType));
            cmd.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
            cmd.Parameters.AddWithValue("travelcard_valid_to", request.TravelcardValidTo);
            cmd.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("travelcard_number", request.TravelcardNumber);
            cmd.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
            cmd.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference);
            cmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);
            var travelcardId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            foreach (var ch in request.Cardholders)
            {
                await using var ccmd = new NpgsqlCommand(@"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key) VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);", conn, tx);
                ccmd.Parameters.AddWithValue("travelcard_id", travelcardId);
                ccmd.Parameters.AddWithValue("cardholder_title", ch.CardholderTitle);
                ccmd.Parameters.AddWithValue("cardholder_forename", ch.CardholderForename);
                ccmd.Parameters.AddWithValue("cardholder_surname", ch.CardholderSurname);
                ccmd.Parameters.Add(new NpgsqlParameter("cardholder_type", ch.CardholderType));
                ccmd.Parameters.AddWithValue("cardholder_photo_name", ch.CardholderPhotoName);
                ccmd.Parameters.AddWithValue("cardholder_photo_rrs_key", (object?)ch.CardholderPhotoRRSKey ?? DBNull.Value);
                ccmd.Parameters.AddWithValue("cardholder_photo_url", (object?)ch.CardholderPhotoURL ?? DBNull.Value);
                ccmd.Parameters.AddWithValue("cardholder_photo_key", (object?)ch.CardholderPhotoKey ?? DBNull.Value);
                await ccmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return new Response { TravelcardId = travelcardId.ToString(), Token = GenerateToken() };
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private string BuildConnectionString()
    {
        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = SecretsHelper.Get("host", "POSTGRESQLHOST"),
            Port = int.Parse(SecretsHelper.Get("port", "POSTGRESQLPORT")),
            Database = SecretsHelper.Get("dbname", "POSTGRESQLDATABASE"),
            Username = SecretsHelper.Get("username", "POSTGRESQLUSERNAME"),
            Password = SecretsHelper.Get("password", "POSTGRESQLPASSWORD")
        };
        return csb.ConnectionString;
    }

    private static string GenerateToken() => Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", "").Replace("/", "").Replace("+", "").Substring(0, 6).ToUpperInvariant();

    private static ValidationResult Fail(string message) => new(false, message);
}

public sealed record ValidationResult(bool IsValid, string? ErrorMessage)
{
    public static ValidationResult Ok() => new(true, null);
}