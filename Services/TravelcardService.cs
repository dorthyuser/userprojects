using Npgsql;
using Npgsql.NameTranslation;
using azuretravelcardapi907.Models;

namespace azuretravelcardapi907.Services;

public class TravelcardService : ITravelcardService
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<TravelcardService> _logger;

    public TravelcardService(ILogger<TravelcardService> logger)
    {
        _logger = logger;
        var host = SecretHelper.Get("postgresql-host", "POSTGRESQLHOST");
        var port = SecretHelper.Get("postgresql-port", "POSTGRESQLPORT");
        var database = SecretHelper.Get("postgresql-database", "POSTGRESQLDATABASE");
        var username = SecretHelper.Get("postgresql-username", "POSTGRESQLUSERNAME");
        var password = SecretHelper.Get("postgresql-password", "POSTGRESQLPASSWORD");
        var connStr = $"Host={host};Port={port};Database={database};Username={username};Password={password};";
        var builder = new NpgsqlDataSourceBuilder(connStr);
        builder.MapEnum<TravelcardTypeEnum>("travelcard_type_enum", nameTranslator: new NpgsqlNullNameTranslator());
        builder.MapEnum<CardholderTypeEnum>("cardholder_type_enum", nameTranslator: new NpgsqlNullNameTranslator());
        _dataSource = builder.Build();
    }

    public async Task<CreateTravelcardResponse> CreateAsync(CreateTravelcardRequest request, string? correlationId, CancellationToken cancellationToken)
    {
        Validate(request);
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            var travelcardId = 0;
            await using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT INTO travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to) VALUES (@type::travelcard_type_enum, @validfrom, @validto, @name, @number, @requesteddate, @reference, @usableto) RETURNING id";
                cmd.Parameters.AddWithValue("type", request.TravelcardType.ToString());
                cmd.Parameters.AddWithValue("validfrom", request.TravelcardValidFrom);
                cmd.Parameters.AddWithValue("validto", request.TravelcardValidTo);
                cmd.Parameters.AddWithValue("name", (object?)request.TravelcardName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("number", request.TravelcardNumber);
                cmd.Parameters.AddWithValue("requesteddate", request.TravelcardRequestedDate);
                cmd.Parameters.AddWithValue("reference", request.TravelcardTransactionReference);
                cmd.Parameters.AddWithValue("usableto", (object?)request.TravelcardUsableTo ?? DBNull.Value);
                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                travelcardId = Convert.ToInt32(result);
            }
            foreach (var cardholder in request.Cardholders)
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT INTO cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key) VALUES (@travelcardid, @title, @forename, @surname, @type::cardholder_type_enum, @photoname, @rrskey, @url, @photokey)";
                cmd.Parameters.AddWithValue("travelcardid", travelcardId);
                cmd.Parameters.AddWithValue("title", cardholder.CardholderTitle);
                cmd.Parameters.AddWithValue("forename", cardholder.CardholderForename);
                cmd.Parameters.AddWithValue("surname", cardholder.CardholderSurname);
                cmd.Parameters.AddWithValue("type", cardholder.CardholderType.ToString());
                cmd.Parameters.AddWithValue("photoname", cardholder.CardholderPhotoName);
                cmd.Parameters.AddWithValue("rrskey", (object?)cardholder.CardholderPhotoRRSKey ?? DBNull.Value);
                cmd.Parameters.AddWithValue("url", (object?)cardholder.CardholderPhotoURL ?? DBNull.Value);
                cmd.Parameters.AddWithValue("photokey", (object?)cardholder.CardholderPhotoKey ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            await tx.CommitAsync(cancellationToken);
            return new CreateTravelcardResponse { TravelcardId = Guid.NewGuid().ToString(), Token = Random.Shared.Next(100000, 999999).ToString() };
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static void Validate(CreateTravelcardRequest request)
    {
        if (request.TravelcardRequestedDate >= DateTimeOffset.UtcNow)
            throw new ArgumentException("requested_date must be in the past");
        if (request.TravelcardValidFrom >= request.TravelcardValidTo)
            throw new ArgumentException("valid_from must be before valid_to");
        if (request.TravelcardValidTo <= DateTimeOffset.UtcNow)
            throw new ArgumentException("valid_to must be in the future");
        if (request.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen && request.TravelcardUsableTo == null)
            throw new ArgumentException("usable_to is required for SixteenToSeventeen");
        if (request.TravelcardUsableTo != null && request.TravelcardUsableTo <= DateTimeOffset.UtcNow)
            throw new ArgumentException("usable_to must be in the future");
        var secondaryCardholders = request.Cardholders.Where(c => c.CardholderType == CardholderTypeEnum.Secondary).ToList();
        if (secondaryCardholders.Any() && (request.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen || request.TravelcardType == TravelcardTypeEnum.Veterans))
            throw new ArgumentException("Secondary cardholder not allowed for this travelcard type");
        foreach (var cardholder in request.Cardholders)
        {
            var photoCount = new[] { cardholder.CardholderPhotoRRSKey, cardholder.CardholderPhotoURL, cardholder.CardholderPhotoKey }.Count(p => !string.IsNullOrWhiteSpace(p));
            if (photoCount != 1)
                throw new ArgumentException("Each cardholder must provide exactly one of: CardholderPhotoRRSKey, CardholderPhotoURL, or CardholderPhotoKey");
        }
        if (request.Cardholders.Count < 1 || request.Cardholders.Count > 2)
            throw new ArgumentException("cardholders must contain exactly one or two items");
        if (!request.Cardholders.Any(c => c.CardholderType == CardholderTypeEnum.Primary))
            throw new ArgumentException("Exactly one Primary cardholder is required");
        if (request.Cardholders.Count(c => c.CardholderType == CardholderTypeEnum.Primary) != 1)
            throw new ArgumentException("Exactly one Primary cardholder is required");
    }
}