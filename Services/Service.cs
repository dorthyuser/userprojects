using System.Text.RegularExpressions;
using Amazon.Lambda.Core;
using Npgsql;
using Npgsql.NameTranslation;
using TravelcardAppDemoLambda.Enums;
using TravelcardAppDemoLambda.Models;

namespace TravelcardAppDemoLambda.Services;

public sealed class Service
{
    private readonly NpgsqlDataSource _dataSource;

    public Service()
    {
        var builder = new NpgsqlDataSourceBuilder(BuildConnectionString());
        builder.MapEnum<TravelcardType>("travelcard_type_enum", new NpgsqlNullNameTranslator());
        builder.MapEnum<CardholderType>("cardholder_type_enum", new NpgsqlNullNameTranslator());
        _dataSource = builder.Build();
    }

    public async Task<ResponseModel> CreateTravelcardAsync(RequestModel request, ILambdaContext context)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var tx = await connection.BeginTransactionAsync();
        try
        {
            const string sql = @"INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to) VALUES (@travelcard_type, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to) RETURNING id;";
            await using var cmd = new NpgsqlCommand(sql, connection, tx);
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
                const string cardholderSql = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key) VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";
                await using var cardholderCmd = new NpgsqlCommand(cardholderSql, connection, tx);
                cardholderCmd.Parameters.AddWithValue("travelcard_id", travelcardId);
                cardholderCmd.Parameters.AddWithValue("cardholder_title", cardholder.CardholderTitle);
                cardholderCmd.Parameters.AddWithValue("cardholder_forename", cardholder.CardholderForename);
                cardholderCmd.Parameters.AddWithValue("cardholder_surname", cardholder.CardholderSurname);
                cardholderCmd.Parameters.Add(new NpgsqlParameter("cardholder_type", cardholder.CardholderType));
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_name", cardholder.CardholderPhotoName);
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_rrs_key", (object?)cardholder.CardholderPhotoRRSKey ?? DBNull.Value);
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_url", (object?)cardholder.CardholderPhotoURL ?? DBNull.Value);
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_key", (object?)cardholder.CardholderPhotoKey ?? DBNull.Value);
                await cardholderCmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return new ResponseModel { TravelcardId = travelcardId.ToString(), Token = GenerateToken() };
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static string BuildConnectionString()
    {
        var host = SecretsHelper.Get("host", "POSTGRESQLHOST");
        var port = SecretsHelper.Get("port", "POSTGRESQLPORT");
        var db = SecretsHelper.Get("dbname", "POSTGRESQLDATABASE");
        var user = SecretsHelper.Get("username", "POSTGRESQLUSERNAME");
        var pwd = SecretsHelper.Get("password", "POSTGRESQLPASSWORD");
        return $"Host={host};Port={port};Database={db};Username={user};Password={pwd};Pooling=true;SSL Mode=Require;Trust Server Certificate=true";
    }

    private static string GenerateToken() => Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", string.Empty).Replace("+", string.Empty).Replace("/", string.Empty)[..6];
}

internal static class RequestModelValidator
{
    private static readonly Regex NameRegex = new(@"^[A-Za-z0-9 ]*$", RegexOptions.Compiled);
    private static readonly Regex AlphaNumRegex = new(@"^[A-Za-z0-9]+$", RegexOptions.Compiled);
    private static readonly Regex TitleRegex = new(@"^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-žºª .''’\-]+$", RegexOptions.Compiled);
    private static readonly Regex NameStrictRegex = new(@"^(?!.*[×÷ˇ˘μ])[A-Za-z0-9À-ž _.\-()\[\]'’,&#+]+$", RegexOptions.Compiled);

    public static ValidationError? Validate(RequestModel model)
    {
        if (model.TravelcardRequestedDate >= DateTimeOffset.UtcNow) return new ValidationError("travelcardRequestedDate", "must be in the past");
        if (model.TravelcardValidFrom <= model.TravelcardValidTo) return new ValidationError("travelcardValidFrom", "must be later than travelcardValidTo");
        if (model.TravelcardValidTo <= DateTimeOffset.UtcNow) return new ValidationError("travelcardValidTo", "must be in the future");
        if (model.TravelcardType == TravelcardType.SixteenToSeventeen && model.TravelcardUsableTo == null) return new ValidationError("travelcardUsableTo", "required for SixteenToSeventeen");
        if (model.TravelcardUsableTo != null && model.TravelcardUsableTo <= DateTimeOffset.UtcNow) return new ValidationError("travelcardUsableTo", "must be in the future");
        if (model.TravelcardType != TravelcardType.SixteenToSeventeen && model.TravelcardUsableTo != null) return new ValidationError("travelcardUsableTo", "not allowed for this travelcard type");
        if (model.Cardholders.Count < 1 || model.Cardholders.Count > 2) return new ValidationError("cardholders", "must contain exactly one primary and optional one secondary");
        if (model.Cardholders.Count(x => x.CardholderType == CardholderType.Primary) != 1) return new ValidationError("cardholderType", "exactly one primary cardholder required");
        if (model.Cardholders.Any(x => x.CardholderType == CardholderType.Secondary) && !IsSecondaryAllowed(model.TravelcardType)) return new ValidationError("cardholderType", "secondary cardholder not allowed for travelcard type");
        if (model.TravelcardName != null && (!IsLength(model.TravelcardName, 255) || !NameRegex.IsMatch(model.TravelcardName))) return new ValidationError("travelcardName", "invalid format");
        if (!IsLength(model.TravelcardNumber, 11, 22) || !AlphaNumRegex.IsMatch(model.TravelcardNumber)) return new ValidationError("travelcardNumber", "invalid format");
        if (!IsLength(model.TravelcardTransactionReference, 15) || !Regex.IsMatch(model.TravelcardTransactionReference, @"^[0-9]{2}[A-Z0-9]{4}[0-9]{4}[0-9]{5}$")) return new ValidationError("travelcardTransactionReference", "invalid format");
        foreach (var ch in model.Cardholders)
        {
            if (!IsLength(ch.CardholderTitle, 1, 15) || !TitleRegex.IsMatch(ch.CardholderTitle)) return new ValidationError("cardholderTitle", "invalid format");
            if (!IsLength(ch.CardholderForename, 1, 100)) return new ValidationError("cardholderForename", "invalid format");
            if (!IsLength(ch.CardholderSurname, 1, 100)) return new ValidationError("cardholderSurname", "invalid format");
            if (!IsLength(ch.CardholderPhotoName, 1, 100) || !NameStrictRegex.IsMatch(ch.CardholderPhotoName)) return new ValidationError("cardholderPhotoName", "invalid format");
            var oneOfCount = new[] { ch.CardholderPhotoRRSKey, ch.CardholderPhotoURL, ch.CardholderPhotoKey }.Count(v => !string.IsNullOrWhiteSpace(v));
            if (oneOfCount != 1) return new ValidationError("cardholderPhoto", "exactly one photo field required");
        }
        return null;
    }

    private static bool IsSecondaryAllowed(TravelcardType type) => type is TravelcardType.TwoTogether or TravelcardType.Family;
    private static bool IsLength(string value, int min, int max = int.MaxValue) => value.Length >= min && value.Length <= max;
}