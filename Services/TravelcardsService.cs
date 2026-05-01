using azuretravelcardapi121.Models;
using Npgsql;
using Npgsql.NameTranslation;

namespace azuretravelcardapi121.Services;

public sealed class TravelcardsService : ITravelcardsService
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

    public async Task<TravelcardResponse> CreateAsync(TravelcardCreateRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating request...");
        Validate(request);
        _logger.LogInformation("Validation passed.");

        var travelcardId = Guid.NewGuid();
        var token = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"INSERT INTO travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to) VALUES (@type::travelcard_type_enum, @valid_from, @valid_to, @name, @number, @requested_date, @transaction_reference, @usable_to) RETURNING id";
            cmd.Parameters.AddWithValue("type", request.TravelcardType.ToString());
            cmd.Parameters.AddWithValue("valid_from", request.TravelcardValidFrom);
            cmd.Parameters.AddWithValue("valid_to", request.TravelcardValidTo);
            cmd.Parameters.AddWithValue("name", (object?)request.TravelcardName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("number", request.TravelcardNumber);
            cmd.Parameters.AddWithValue("requested_date", request.TravelcardRequestedDate);
            cmd.Parameters.AddWithValue("transaction_reference", request.TravelcardTransactionReference);
            cmd.Parameters.AddWithValue("usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);

            var id = Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken));

            foreach (var cardholder in request.Cardholders)
            {
                await using var chCmd = conn.CreateCommand();
                chCmd.Transaction = tx;
                chCmd.CommandText = @"INSERT INTO cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key) VALUES (@travelcard_id, @title, @forename, @surname, @type::cardholder_type_enum, @photo_name, @photo_rrs_key, @photo_url, @photo_key)";
                chCmd.Parameters.AddWithValue("travelcard_id", id);
                chCmd.Parameters.AddWithValue("title", cardholder.CardholderTitle);
                chCmd.Parameters.AddWithValue("forename", cardholder.CardholderForename);
                chCmd.Parameters.AddWithValue("surname", cardholder.CardholderSurname);
                chCmd.Parameters.AddWithValue("type", cardholder.CardholderType.ToString());
                chCmd.Parameters.AddWithValue("photo_name", cardholder.CardholderPhotoName);
                chCmd.Parameters.AddWithValue("photo_rrs_key", (object?)cardholder.CardholderPhotoRRSKey ?? DBNull.Value);
                chCmd.Parameters.AddWithValue("photo_url", (object?)cardholder.CardholderPhotoURL ?? DBNull.Value);
                chCmd.Parameters.AddWithValue("photo_key", (object?)cardholder.CardholderPhotoKey ?? DBNull.Value);
                await chCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            return new TravelcardResponse { TravelcardId = travelcardId, Token = token };
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static void Validate(TravelcardCreateRequest request)
    {
        if (request.TravelcardRequestedDate >= DateTimeOffset.UtcNow) throw new ArgumentException("requested_date must be in past");
        if (request.TravelcardValidFrom >= request.TravelcardValidTo) throw new ArgumentException("valid_from must be before valid_to");
        if (request.TravelcardValidTo <= DateTimeOffset.UtcNow) throw new ArgumentException("valid_to must be in future");
        if (request.TravelcardUsableTo.HasValue && request.TravelcardUsableTo.Value <= DateTimeOffset.UtcNow) throw new ArgumentException("usable_to must be in future");
        if (request.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen && !request.TravelcardUsableTo.HasValue) throw new ArgumentException("usable_to is required for SixteenToSeventeen");
        if (request.TravelcardType != TravelcardTypeEnum.SixteenToSeventeen && request.TravelcardUsableTo.HasValue) throw new ArgumentException("usable_to only allowed for SixteenToSeventeen");
        if (request.Cardholders.Count is < 1 or > 2) throw new ArgumentException("cardholders must contain exactly one or two items");
        if (request.Cardholders.Count(ch => ch.CardholderType == CardholderTypeEnum.Primary) != 1) throw new ArgumentException("Exactly one Primary cardholder is required");
        if (request.Cardholders.Any(ch => ch.CardholderType == CardholderTypeEnum.Secondary) && request.TravelcardType == TravelcardTypeEnum.Senior) throw new ArgumentException("Secondary not allowed for this type");
        foreach (var ch in request.Cardholders)
        {
            var provided = new[] { ch.CardholderPhotoRRSKey, ch.CardholderPhotoURL, ch.CardholderPhotoKey }.Count(x => !string.IsNullOrWhiteSpace(x));
            if (provided != 1) throw new ArgumentException("Exactly one of cardholderPhotoRRSKey, cardholderPhotoURL, cardholderPhotoKey is required");
        }
    }
}