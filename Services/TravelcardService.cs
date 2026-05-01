using Npgsql;
using Npgsql.NameTranslation;
using new_test_for_demo.Models;

namespace new_test_for_demo.Services;

public class TravelcardService : ITravelcardService
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<TravelcardService> _logger;

    public TravelcardService(ILogger<TravelcardService> logger)
    {
        _logger = logger;
        var host = SecretHelper.Get("postgresql-host", "POSTGRESQL_HOST");
        var port = SecretHelper.Get("postgresql-port", "POSTGRESQL_PORT");
        var database = SecretHelper.Get("postgresql-database", "POSTGRESQL_DATABASE");
        var username = SecretHelper.Get("postgresql-username", "POSTGRESQL_USERNAME");
        var password = SecretHelper.Get("postgresql-password", "POSTGRESQL_PASSWORD");
        var connStr = $"Host={host};Port={port};Database={database};Username={username};Password={password};";
        var builder = new NpgsqlDataSourceBuilder(connStr);
        builder.MapEnum<TravelcardTypeEnum>("travelcard_type_enum", nameTranslator: new NpgsqlNullNameTranslator());
        builder.MapEnum<CardholderTypeEnum>("cardholder_type_enum", nameTranslator: new NpgsqlNullNameTranslator());
        _dataSource = builder.Build();
    }

    public async Task<CreateTravelcardResponse> CreateAsync(CreateTravelcardRequest request)
    {
        if (request.TravelcardRequestedDate >= DateTimeOffset.UtcNow)
            throw new ArgumentException("requestedDate must be in the past");
        if (request.TravelcardValidFrom >= request.TravelcardValidTo)
            throw new ArgumentException("validFrom must be before validTo");
        if (request.TravelcardValidTo <= DateTimeOffset.UtcNow)
            throw new ArgumentException("validTo must be in the future");
        if (request.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen && request.TravelcardUsableTo == null)
            throw new ArgumentException("usableTo is required for SixteenToSeventeen");
        if (request.TravelcardUsableTo != null && request.TravelcardUsableTo <= DateTimeOffset.UtcNow)
            throw new ArgumentException("usableTo must be in the future");
        var secondaryCardholders = request.Cardholders.Where(c => c.CardholderType == CardholderTypeEnum.Secondary).ToList();
        if (secondaryCardholders.Any() && (request.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen || request.TravelcardType == TravelcardTypeEnum.Veterans))
            throw new ArgumentException("Secondary cardholder not allowed for this travelcard type");
        foreach (var cardholder in request.Cardholders)
        {
            var photoCount = new[] { cardholder.CardholderPhotoRRSKey, cardholder.CardholderPhotoURL, cardholder.CardholderPhotoKey }.Count(p => !string.IsNullOrWhiteSpace(p));
            if (photoCount != 1)
                throw new ArgumentException("Each cardholder must provide exactly one of: CardholderPhotoRRSKey, CardholderPhotoURL, or CardholderPhotoKey");
        }

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"INSERT INTO travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to) VALUES (@type::travelcard_type_enum, @validFrom, @validTo, @name, @number, @requestedDate, @transactionReference, @usableTo) RETURNING id";
            cmd.Parameters.AddWithValue("type", request.TravelcardType);
            cmd.Parameters.AddWithValue("validFrom", request.TravelcardValidFrom);
            cmd.Parameters.AddWithValue("validTo", request.TravelcardValidTo);
            cmd.Parameters.AddWithValue("name", (object?)request.TravelcardName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("number", request.TravelcardNumber);
            cmd.Parameters.AddWithValue("requestedDate", request.TravelcardRequestedDate);
            cmd.Parameters.AddWithValue("transactionReference", request.TravelcardTransactionReference);
            cmd.Parameters.AddWithValue("usableTo", (object?)request.TravelcardUsableTo ?? DBNull.Value);
            var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            foreach (var cardholder in request.Cardholders)
            {
                await using var chCmd = conn.CreateCommand();
                chCmd.Transaction = tx;
                chCmd.CommandText = @"INSERT INTO cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key) VALUES (@travelcardId, @title, @forename, @surname, @type::cardholder_type_enum, @photoName, @photoRrsKey, @photoUrl, @photoKey)";
                chCmd.Parameters.AddWithValue("travelcardId", id);
                chCmd.Parameters.AddWithValue("title", cardholder.CardholderTitle);
                chCmd.Parameters.AddWithValue("forename", cardholder.CardholderForename);
                chCmd.Parameters.AddWithValue("surname", cardholder.CardholderSurname);
                chCmd.Parameters.AddWithValue("type", cardholder.CardholderType);
                chCmd.Parameters.AddWithValue("photoName", cardholder.CardholderPhotoName);
                chCmd.Parameters.AddWithValue("photoRrsKey", (object?)cardholder.CardholderPhotoRRSKey ?? DBNull.Value);
                chCmd.Parameters.AddWithValue("photoUrl", (object?)cardholder.CardholderPhotoURL ?? DBNull.Value);
                chCmd.Parameters.AddWithValue("photoKey", (object?)cardholder.CardholderPhotoKey ?? DBNull.Value);
                await chCmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return new CreateTravelcardResponse { TravelcardId = id.ToString(), Token = GenerateToken() };
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static string GenerateToken() => Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", string.Empty).Replace("+", string.Empty).Replace("/", string.Empty).Substring(0, 6).ToUpperInvariant();
}