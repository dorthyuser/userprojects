using System.Text.RegularExpressions;
using demo_travelcard_aus.Models;
using Npgsql;
using Npgsql.NameTranslation;

namespace demo_travelcard_aus.Services;

public class TravelcardService : ITravelcardService
{
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

    public async Task<TravelcardResponse> CreateAsync(TravelcardCreateRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating request...");
        Validate(request);
        _logger.LogInformation("Validation passed.");

        var token = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        int travelcardId;
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Inserting {Resource} into DB...", "Travelcard");
            await using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT INTO travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to) VALUES (@type, @validFrom, @validTo, @name, @number, @requestedDate, @transactionReference, @usableTo) RETURNING id;";
                cmd.Parameters.AddWithValue("type", request.TravelcardType);
                cmd.Parameters.AddWithValue("validFrom", request.TravelcardValidFrom);
                cmd.Parameters.AddWithValue("validTo", request.TravelcardValidTo);
                cmd.Parameters.AddWithValue("name", (object?)request.TravelcardName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("number", request.TravelcardNumber);
                cmd.Parameters.AddWithValue("requestedDate", request.TravelcardRequestedDate);
                cmd.Parameters.AddWithValue("transactionReference", request.TravelcardTransactionReference);
                cmd.Parameters.AddWithValue("usableTo", (object?)request.TravelcardUsableTo ?? DBNull.Value);
                travelcardId = (int)(await cmd.ExecuteScalarAsync(cancellationToken) ?? throw new InvalidOperationException("Insert failed"));
            }

            foreach (var cardholder in request.Cardholders)
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT INTO cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key) VALUES (@travelcardId, @title, @forename, @surname, @type, @photoName, @rrsKey, @url, @photoKey);";
                cmd.Parameters.AddWithValue("travelcardId", travelcardId);
                cmd.Parameters.AddWithValue("title", cardholder.CardholderTitle);
                cmd.Parameters.AddWithValue("forename", cardholder.CardholderForename);
                cmd.Parameters.AddWithValue("surname", cardholder.CardholderSurname);
                cmd.Parameters.AddWithValue("type", cardholder.CardholderType);
                cmd.Parameters.AddWithValue("photoName", cardholder.CardholderPhotoName);
                cmd.Parameters.AddWithValue("rrsKey", (object?)cardholder.CardholderPhotoRRSKey ?? DBNull.Value);
                cmd.Parameters.AddWithValue("url", (object?)cardholder.CardholderPhotoURL ?? DBNull.Value);
                cmd.Parameters.AddWithValue("photoKey", (object?)cardholder.CardholderPhotoKey ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            _logger.LogInformation("Travelcard inserted. Id={Id}", travelcardId);
            return new TravelcardResponse { TravelcardId = travelcardId.ToString(), Token = token };
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            _logger.LogError(new Exception("DB insert failed"), "DB error inserting {Resource}", "Travelcard");
            throw;
        }
    }

    private static void Validate(TravelcardCreateRequest req)
    {
        var now = DateTimeOffset.UtcNow;
        if (req.TravelcardRequestedDate >= now) throw new ArgumentException("travelcardRequestedDate must be in the past");
        if (req.TravelcardValidFrom >= req.TravelcardValidTo) throw new ArgumentException("travelcardValidFrom must be later than travelcardValidTo");
        if (req.TravelcardValidTo <= now) throw new ArgumentException("travelcardValidTo must be in the future");
        if (req.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen && req.TravelcardUsableTo == null) throw new ArgumentException("travelcardUsableTo is required for SixteenToSeventeen");
        if (req.TravelcardUsableTo != null && req.TravelcardUsableTo <= now) throw new ArgumentException("travelcardUsableTo must be in the future");
        if (req.Cardholders == null || req.Cardholders.Count < 1 || req.Cardholders.Count > 2) throw new ArgumentException("cardholders must contain 1 or 2 items");

        var primaryCount = req.Cardholders.Count(c => c.CardholderType == CardholderTypeEnum.Primary);
        var secondaryCount = req.Cardholders.Count(c => c.CardholderType == CardholderTypeEnum.Secondary);
        if (primaryCount != 1) throw new ArgumentException("Exactly one Primary cardholder is required");
        if (secondaryCount > 1) throw new ArgumentException("Only one Secondary cardholder is allowed");
        if (secondaryCount == 1 && req.TravelcardType is TravelcardTypeEnum.Young or TravelcardTypeEnum.Senior or TravelcardTypeEnum.Veterans)
            throw new ArgumentException("Secondary cardholder is not allowed for this travelcard type");

        foreach (var c in req.Cardholders)
        {
            var oneOf = new[] { c.CardholderPhotoRRSKey, c.CardholderPhotoURL, c.CardholderPhotoKey }.Count(x => !string.IsNullOrWhiteSpace(x));
            if (oneOf != 1) throw new ArgumentException("Exactly one of cardholderPhotoRRSKey, cardholderPhotoURL, cardholderPhotoKey is required");
            if (!string.IsNullOrWhiteSpace(c.CardholderPhotoRRSKey) && !Regex.IsMatch(c.CardholderPhotoRRSKey, @"^[A-Za-z0-9-]{36}\.[A-Za-z0-9]{2,5}$")) throw new ArgumentException("cardholderPhotoRRSKey invalid");
            if (!string.IsNullOrWhiteSpace(c.CardholderPhotoKey) && !Regex.IsMatch(c.CardholderPhotoKey, @"^[A-Za-z0-9-]{36}\.[A-Za-z0-9]{2,5}$")) throw new ArgumentException("cardholderPhotoKey invalid");
        }
    }
}