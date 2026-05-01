using demo_travelcard_paul.Models;
using Npgsql;
using Npgsql.NameTranslation;
using System.Text.RegularExpressions;

namespace demo_travelcard_paul.Services;

public class TravelcardsService : ITravelcardsService
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<TravelcardsService> _logger;

    public TravelcardsService(ILogger<TravelcardsService> logger)
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

    public async Task<CreateTravelcardResponse> CreateAsync(CreateTravelcardRequest request)
    {
        _logger.LogInformation("Validating request...");
        Validate(request);
        _logger.LogInformation("Validation passed.");
        _logger.LogInformation("Inserting {Resource} into DB...", "Travelcard");

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            int travelcardId;
            await using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT INTO travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to) VALUES (@type::travelcard_type_enum, @validfrom, @validto, @name, @number, @requested, @reference, @usableto) RETURNING id";
                cmd.Parameters.AddWithValue("type", request.TravelcardType);
                cmd.Parameters.AddWithValue("validfrom", request.TravelcardValidFrom);
                cmd.Parameters.AddWithValue("validto", request.TravelcardValidTo);
                cmd.Parameters.AddWithValue("name", (object?)request.TravelcardName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("number", request.TravelcardNumber);
                cmd.Parameters.AddWithValue("requested", request.TravelcardRequestedDate);
                cmd.Parameters.AddWithValue("reference", request.TravelcardTransactionReference);
                cmd.Parameters.AddWithValue("usableto", (object?)request.TravelcardUsableTo ?? DBNull.Value);
                travelcardId = (int)(await cmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("Insert failed"));
            }

            foreach (var ch in request.Cardholders)
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT INTO cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key) VALUES (@travelcard_id, @title, @forename, @surname, @type::cardholder_type_enum, @photo_name, @rrs_key, @photo_url, @photo_key)";
                cmd.Parameters.AddWithValue("travelcard_id", travelcardId);
                cmd.Parameters.AddWithValue("title", ch.CardholderTitle);
                cmd.Parameters.AddWithValue("forename", ch.CardholderForename);
                cmd.Parameters.AddWithValue("surname", ch.CardholderSurname);
                cmd.Parameters.AddWithValue("type", ch.CardholderType);
                cmd.Parameters.AddWithValue("photo_name", ch.CardholderPhotoName);
                cmd.Parameters.AddWithValue("rrs_key", (object?)ch.CardholderPhotoRRSKey ?? DBNull.Value);
                cmd.Parameters.AddWithValue("photo_url", (object?)ch.CardholderPhotoURL ?? DBNull.Value);
                cmd.Parameters.AddWithValue("photo_key", (object?)ch.CardholderPhotoKey ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
            }

            var token = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            await tx.CommitAsync();
            _logger.LogInformation("Travelcard inserted. Id={Id}", travelcardId);
            _logger.LogInformation("Request completed successfully. Id={Id}", travelcardId);
            return new CreateTravelcardResponse { TravelcardId = travelcardId, Token = token };
        }
        catch
        {
            await tx.RollbackAsync();
            _logger.LogError(new Exception(), "DB error inserting {Resource}", "Travelcard");
            throw;
        }
    }

    private static void Validate(CreateTravelcardRequest req)
    {
        if (req.TravelcardRequestedDate >= DateTimeOffset.UtcNow) throw new ArgumentException("requested_date must be in the past");
        if (req.TravelcardValidFrom >= req.TravelcardValidTo) throw new ArgumentException("valid_from must be before valid_to");
        if (req.TravelcardValidTo <= DateTimeOffset.UtcNow) throw new ArgumentException("valid_to must be in the future");
        if (req.TravelcardUsableTo.HasValue && req.TravelcardUsableTo.Value <= DateTimeOffset.UtcNow) throw new ArgumentException("usable_to must be in the future");
        if (req.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen && !req.TravelcardUsableTo.HasValue) throw new ArgumentException("usable_to is required for SixteenToSeventeen");
        if (req.TravelcardType != TravelcardTypeEnum.SixteenToSeventeen && req.TravelcardUsableTo.HasValue) throw new ArgumentException("usable_to only allowed for SixteenToSeventeen");
        if (req.TravelcardValidFrom > req.TravelcardRequestedDate.AddMonths(1)) throw new ArgumentException("valid_from must be no later than one month from creation date");
        if (req.Cardholders.Count < 1 || req.Cardholders.Count > 2) throw new ArgumentException("cardholders must contain 1 or 2 items");
        if (req.Cardholders.Count(c => c.CardholderType == CardholderTypeEnum.Primary) != 1) throw new ArgumentException("Exactly one Primary cardholder is required");
        if (req.Cardholders.Any(c => new[] { c.CardholderPhotoRRSKey, c.CardholderPhotoURL, c.CardholderPhotoKey }.Count(v => !string.IsNullOrWhiteSpace(v)) != 1)) throw new ArgumentException("Each cardholder requires exactly one photo identifier");
        if (req.Cardholders.Any(c => c.CardholderType == CardholderTypeEnum.Secondary) && (req.TravelcardType == TravelcardTypeEnum.Young || req.TravelcardType == TravelcardTypeEnum.Senior || req.TravelcardType == TravelcardTypeEnum.Veterans)) throw new ArgumentException("Secondary cardholder not allowed for this travelcard type");
        foreach (var c in req.Cardholders)
        {
            if (string.IsNullOrWhiteSpace(c.CardholderTitle) || c.CardholderTitle.Length > 15) throw new ArgumentException("Invalid cardholder title");
            if (string.IsNullOrWhiteSpace(c.CardholderForename) || c.CardholderForename.Length > 100) throw new ArgumentException("Invalid cardholder forename");
            if (string.IsNullOrWhiteSpace(c.CardholderSurname) || c.CardholderSurname.Length > 100) throw new ArgumentException("Invalid cardholder surname");
            if (string.IsNullOrWhiteSpace(c.CardholderPhotoName) || c.CardholderPhotoName.Length > 100) throw new ArgumentException("Invalid cardholder photo name");
            var photoCount = new[] { c.CardholderPhotoRRSKey, c.CardholderPhotoURL, c.CardholderPhotoKey }.Count(v => !string.IsNullOrWhiteSpace(v));
            if (photoCount != 1) throw new ArgumentException("Exactly one photo field must be provided");
            if (!string.IsNullOrWhiteSpace(c.CardholderPhotoRRSKey) && !Regex.IsMatch(c.CardholderPhotoRRSKey, "^[A-Za-z0-9-]{36}\\.[A-Za-z0-9]{2,5}$")) throw new ArgumentException("Invalid cardholderPhotoRRSKey");
            if (!string.IsNullOrWhiteSpace(c.CardholderPhotoKey) && !Regex.IsMatch(c.CardholderPhotoKey, "^[A-Za-z0-9-]{36}\\.[A-Za-z0-9]{2,5}$")) throw new ArgumentException("Invalid cardholderPhotoKey");
            if (!string.IsNullOrWhiteSpace(c.CardholderPhotoURL) && !Uri.TryCreate(c.CardholderPhotoURL, UriKind.Absolute, out _)) throw new ArgumentException("Invalid cardholderPhotoURL");
        }
    }
}