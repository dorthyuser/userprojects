using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using Npgsql.NameTranslation;
using httptestingapi.Models;

namespace httptestingapi.Services;

public sealed class TravelcardService : ITravelcardService
{
    private static readonly Regex TravelcardNameRegex = new(@"^[A-Za-z0-9 ]*$", RegexOptions.Compiled);
    private static readonly Regex CardholderTitleRegex = new(@"^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-žºª .''’\-]+$", RegexOptions.Compiled);
    private static readonly Regex CardholderForenameRegex = new(@"^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-ž .''’\-]+$", RegexOptions.Compiled);
    private static readonly Regex CardholderSurnameRegex = new(@"^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-ž .''’\-]+$", RegexOptions.Compiled);
    private static readonly Regex CardholderPhotoNameRegex = new(@"^(?!.*[×÷ˇ˘μ])[A-Za-z0-9À-ž _.\-()\[\]'',&+#]+$", RegexOptions.Compiled);
    private static readonly Regex PhotoKeyRegex = new(@"^[A-Za-z0-9-]{36}\.[A-Za-z0-9]{2,5}$", RegexOptions.Compiled);
    private static readonly Regex PhotoUrlRegex = new(@"^(https?://)[A-Za-z0-9._~:/?#@!$&'()*+,;=%-]+$", RegexOptions.Compiled);

    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<TravelcardService> _logger;

    public TravelcardService(ILogger<TravelcardService> logger)
    {
        _logger = logger;
        var host = SecretHelper.Get("POSTGRESQLHOST", "POSTGRESQLHOST");
        var port = SecretHelper.Get("POSTGRESQLPORT", "POSTGRESQLPORT");
        var database = SecretHelper.Get("POSTGRESQLDATABASE", "POSTGRESQLDATABASE");
        var username = SecretHelper.Get("POSTGRESQLUSERNAME", "POSTGRESQLUSERNAME");
        var password = SecretHelper.Get("POSTGRESQLPASSWORD", "POSTGRESQLPASSWORD");
        var connStr = $"Host={host};Port={port};Database={database};Username={username};Password={password};";
        var builder = new NpgsqlDataSourceBuilder(connStr);
        builder.MapEnum<TravelcardTypeEnum>("travelcard_type_enum", nameTranslator: new NpgsqlNullNameTranslator());
        builder.MapEnum<CardholderTypeEnum>("cardholder_type_enum", nameTranslator: new NpgsqlNullNameTranslator());
        _dataSource = builder.Build();
    }

    public async Task<CreateTravelcardResponse> CreateAsync(CreateTravelcardRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating request...");
        Validate(request);
        _logger.LogInformation("Validation passed.");

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            _logger.LogInformation("DB operation: {Operation} into {Table}", "INSERT", "travelcards");
            await using var cmd1 = conn.CreateCommand();
            cmd1.Transaction = tx;
            cmd1.CommandText = @"INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to) VALUES (@travelcard_type, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to) RETURNING id;";
            cmd1.Parameters.AddWithValue("travelcard_type", request.TravelcardType);
            cmd1.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
            cmd1.Parameters.AddWithValue("travelcard_valid_to", request.TravelcardValidTo);
            cmd1.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);
            cmd1.Parameters.AddWithValue("travelcard_number", request.TravelcardNumber);
            cmd1.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
            cmd1.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference);
            cmd1.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);
            var travelcardId = (int)(await cmd1.ExecuteScalarAsync(cancellationToken))!;

            foreach (var cardholder in request.Cardholders)
            {
                _logger.LogInformation("DB operation: {Operation} into {Table}", "INSERT", "cardholders");
                await using var cmd2 = conn.CreateCommand();
                cmd2.Transaction = tx;
                cmd2.CommandText = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key) VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";
                cmd2.Parameters.AddWithValue("travelcard_id", travelcardId);
                cmd2.Parameters.AddWithValue("cardholder_title", cardholder.CardholderTitle);
                cmd2.Parameters.AddWithValue("cardholder_forename", cardholder.CardholderForename);
                cmd2.Parameters.AddWithValue("cardholder_surname", cardholder.CardholderSurname);
                cmd2.Parameters.AddWithValue("cardholder_type", cardholder.CardholderType);
                cmd2.Parameters.AddWithValue("cardholder_photo_name", cardholder.CardholderPhotoName);
                cmd2.Parameters.AddWithValue("cardholder_photo_rrs_key", (object?)cardholder.CardholderPhotoRRSKey ?? DBNull.Value);
                cmd2.Parameters.AddWithValue("cardholder_photo_url", (object?)cardholder.CardholderPhotoURL ?? DBNull.Value);
                cmd2.Parameters.AddWithValue("cardholder_photo_key", (object?)cardholder.CardholderPhotoKey ?? DBNull.Value);
                await cmd2.ExecuteNonQueryAsync(cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            var token = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            var response = new CreateTravelcardResponse { TravelcardId = Guid.NewGuid(), Token = token };
            _logger.LogInformation("DB success: table={Table}, id={Id}", "travelcards", travelcardId);
            _logger.LogInformation("Request completed. id={Id}", travelcardId);
            return response;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "DB error: table={Table}, operation={Operation}", "travelcards", "INSERT");
            throw;
        }
    }

    private static void Validate(CreateTravelcardRequest request)
    {
        if (request.TravelcardRequestedDate >= DateTimeOffset.UtcNow) throw new ArgumentException("requested_date must be in the past");
        if (request.TravelcardValidFrom > DateTimeOffset.UtcNow.AddMonths(1)) throw new ArgumentException("valid_from must be no later than one calendar month from today");
        if (request.TravelcardValidFrom >= request.TravelcardValidTo) throw new ArgumentException("valid_from must be before valid_to");
        if (request.TravelcardValidTo <= DateTimeOffset.UtcNow) throw new ArgumentException("valid_to must be in the future");
        if (request.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen && request.TravelcardUsableTo == null) throw new ArgumentException("usable_to is required for SixteenToSeventeen");
        if (request.TravelcardType != TravelcardTypeEnum.SixteenToSeventeen && request.TravelcardUsableTo != null) throw new ArgumentException("usable_to is only allowed for SixteenToSeventeen");
        if (request.TravelcardUsableTo != null && request.TravelcardUsableTo <= DateTimeOffset.UtcNow) throw new ArgumentException("usable_to must be in the future");
        if (request.TravelcardType is TravelcardTypeEnum.SixteenToSeventeen or TravelcardTypeEnum.Veterans && request.Cardholders.Exists(c => c.CardholderType == CardholderTypeEnum.Secondary)) throw new ArgumentException("secondary cardholder is not allowed for this travelcard type");
        if (request.Cardholders.Count is < 1 or > 2) throw new ArgumentException("cardholders must contain 1 or 2 items");
        var hasPrimary = false;
        var hasSecondary = false;
        foreach (var cardholder in request.Cardholders)
        {
            if (cardholder.CardholderType == CardholderTypeEnum.Primary) hasPrimary = true;
            if (cardholder.CardholderType == CardholderTypeEnum.Secondary) hasSecondary = true;
            if (string.IsNullOrWhiteSpace(cardholder.CardholderTitle) || cardholder.CardholderTitle.Length > 15 || !CardholderTitleRegex.IsMatch(cardholder.CardholderTitle)) throw new ArgumentException("invalid cardholder_title");
            if (string.IsNullOrWhiteSpace(cardholder.CardholderForename) || cardholder.CardholderForename.Length > 100 || !CardholderForenameRegex.IsMatch(cardholder.CardholderForename)) throw new ArgumentException("invalid cardholder_forename");
            if (string.IsNullOrWhiteSpace(cardholder.CardholderSurname) || cardholder.CardholderSurname.Length > 100 || !CardholderSurnameRegex.IsMatch(cardholder.CardholderSurname)) throw new ArgumentException("invalid cardholder_surname");
            if (string.IsNullOrWhiteSpace(cardholder.CardholderPhotoName) || cardholder.CardholderPhotoName.Length > 100 || !CardholderPhotoNameRegex.IsMatch(cardholder.CardholderPhotoName)) throw new ArgumentException("invalid cardholder_photo_name");
            var oneOf = 0;
            if (!string.IsNullOrWhiteSpace(cardholder.CardholderPhotoRRSKey)) { oneOf++; if (cardholder.CardholderPhotoRRSKey.Length < 39 || cardholder.CardholderPhotoRRSKey.Length > 42 || !PhotoKeyRegex.IsMatch(cardholder.CardholderPhotoRRSKey)) throw new ArgumentException("invalid cardholder_photo_rrs_key"); }
            if (!string.IsNullOrWhiteSpace(cardholder.CardholderPhotoURL)) { oneOf++; if (cardholder.CardholderPhotoURL.Length < 20 || cardholder.CardholderPhotoURL.Length > 2048 || !PhotoUrlRegex.IsMatch(cardholder.CardholderPhotoURL)) throw new ArgumentException("invalid cardholder_photo_url"); }
            if (!string.IsNullOrWhiteSpace(cardholder.CardholderPhotoKey)) { oneOf++; if (cardholder.CardholderPhotoKey.Length < 39 || cardholder.CardholderPhotoKey.Length > 42 || !PhotoKeyRegex.IsMatch(cardholder.CardholderPhotoKey)) throw new ArgumentException("invalid cardholder_photo_key"); }
            if (oneOf != 1) throw new ArgumentException("exactly one photo field must be provided");
        }
        if (!hasPrimary) throw new ArgumentException("primary cardholder is required");
        if (request.Cardholders.Count == 2 && !hasSecondary) throw new ArgumentException("secondary cardholder is required for two-item cardholders list");
        if (request.TravelcardName != null && (request.TravelcardName.Length > 255 || !TravelcardNameRegex.IsMatch(request.TravelcardName))) throw new ArgumentException("invalid travelcard_name");
        if (request.TravelcardNumber.Length < 11 || request.TravelcardNumber.Length > 22) throw new ArgumentException("invalid travelcard_number");
        if (request.TravelcardTransactionReference.Length != 15) throw new ArgumentException("invalid travelcard_transaction_reference");
    }
}
