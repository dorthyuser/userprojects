using System.Text.RegularExpressions;
using Npgsql;
using Npgsql.NameTranslation;
using travelcardcsharpsb1114.Models;

namespace travelcardcsharpsb1114.Services;

public sealed class TravelcardService : ITravelcardService
{
    private static readonly Regex TravelcardNameRegex = new(@"^[A-Za-z0-9 ]*$", RegexOptions.Compiled);
    private static readonly Regex CardholderTitleRegex = new(@"^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-žºª .''’\-]+$", RegexOptions.Compiled);
    private static readonly Regex CardholderForenameRegex = new(@"^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-ž .''’\-]+$", RegexOptions.Compiled);
    private static readonly Regex CardholderSurnameRegex = new(@"^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-ž .''’\-]+$", RegexOptions.Compiled);
    private static readonly Regex CardholderPhotoNameRegex = new(@"^(?!.*[×÷ˇ˘μ])[A-Za-z0-9À-ž _\.\-()\[\]'',&+#]+$", RegexOptions.Compiled);
    private static readonly Regex CardholderPhotoRefRegex = new(@"^[A-Za-z0-9-]{36}\.[A-Za-z0-9]{2,5}$", RegexOptions.Compiled);
    private static readonly Regex CardholderPhotoUrlRegex = new(@"^(https?://)[A-Za-z0-9._~:/?#@!$&''()*+,;=%-]+$", RegexOptions.Compiled);
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<TravelcardService> _logger;

    public TravelcardService(ILogger<TravelcardService> logger)
    {
        _logger = logger;
        var host = SecretsHelper.Get("host", "host");
        var port = SecretsHelper.Get("port", "port");
        var database = SecretsHelper.Get("dbname", "dbname");
        var username = SecretsHelper.Get("username", "username");
        var password = SecretsHelper.Get("password", "password");
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
        _logger.LogInformation("Inserting travelcard into DB...");

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var cmd1 = conn.CreateCommand();
            cmd1.Transaction = tx;
            cmd1.CommandText = @"INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to) VALUES (@travelcard_type, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to) RETURNING id";
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
                await using var cmd2 = conn.CreateCommand();
                cmd2.Transaction = tx;
                cmd2.CommandText = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key) VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key)";
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
            _logger.LogInformation("travelcard inserted. Id={Id}", travelcardId);
            _logger.LogInformation("Request completed successfully. Id={Id}", travelcardId);
            return new CreateTravelcardResponse { TravelcardId = Guid.NewGuid(), Token = token };
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "DB error inserting travelcard");
            throw;
        }
    }

    private static void Validate(CreateTravelcardRequest req)
    {
        if (req.TravelcardRequestedDate >= DateTimeOffset.UtcNow) throw new ArgumentException("requested_date must be in the past");
        if (req.TravelcardValidFrom > DateTimeOffset.UtcNow.AddMonths(1)) throw new ArgumentException("valid_from must be no later than one calendar month from today");
        if (req.TravelcardValidFrom >= req.TravelcardValidTo) throw new ArgumentException("validFrom must be before validTo");
        if (req.TravelcardValidTo <= DateTimeOffset.UtcNow) throw new ArgumentException("valid_to must be in the future");
        if (req.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen && req.TravelcardUsableTo == null) throw new ArgumentException("usable_to is required for SixteenToSeventeen");
        if (req.TravelcardType != TravelcardTypeEnum.SixteenToSeventeen && req.TravelcardUsableTo != null) { }
        if (req.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen && req.TravelcardUsableTo != null && req.TravelcardUsableTo <= DateTimeOffset.UtcNow) throw new ArgumentException("usable_to must be in the future");
        if ((req.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen || req.TravelcardType == TravelcardTypeEnum.Veterans) && req.Cardholders.Any(c => c.CardholderType == CardholderTypeEnum.Secondary)) throw new ArgumentException("Secondary cardholder is not allowed for this travelcard type");
        if (req.Cardholders is null || req.Cardholders.Count < 1 || req.Cardholders.Count > 2) throw new ArgumentException("cardholders must contain exactly one or two items");
        if (req.Cardholders.Count(c => c.CardholderType == CardholderTypeEnum.Primary) != 1) throw new ArgumentException("Exactly one Primary cardholder is required");
        if (req.Cardholders.Count(c => c.CardholderType == CardholderTypeEnum.Secondary) > 1) throw new ArgumentException("Only one Secondary cardholder is allowed");
        foreach (var cardholder in req.Cardholders)
        {
            if (!CardholderTitleRegex.IsMatch(cardholder.CardholderTitle)) throw new ArgumentException("Invalid cardholder title format");
            if (!CardholderForenameRegex.IsMatch(cardholder.CardholderForename)) throw new ArgumentException("Invalid cardholder forename format");
            if (!CardholderSurnameRegex.IsMatch(cardholder.CardholderSurname)) throw new ArgumentException("Invalid cardholder surname format");
            if (!CardholderPhotoNameRegex.IsMatch(cardholder.CardholderPhotoName)) throw new ArgumentException("Invalid cardholder photo name format");
            var count = 0;
            if (!string.IsNullOrWhiteSpace(cardholder.CardholderPhotoRRSKey)) count++;
            if (!string.IsNullOrWhiteSpace(cardholder.CardholderPhotoURL)) count++;
            if (!string.IsNullOrWhiteSpace(cardholder.CardholderPhotoKey)) count++;
            if (count != 1) throw new ArgumentException("Exactly one cardholder photo detail must be provided");
            if (cardholder.CardholderPhotoRRSKey != null && !CardholderPhotoRefRegex.IsMatch(cardholder.CardholderPhotoRRSKey)) throw new ArgumentException("Invalid cardholder photo rrs key format");
            if (cardholder.CardholderPhotoURL != null && !CardholderPhotoUrlRegex.IsMatch(cardholder.CardholderPhotoURL)) throw new ArgumentException("Invalid cardholder photo url format");
            if (cardholder.CardholderPhotoKey != null && !CardholderPhotoRefRegex.IsMatch(cardholder.CardholderPhotoKey)) throw new ArgumentException("Invalid cardholder photo key format");
        }
        if (req.TravelcardName != null && !TravelcardNameRegex.IsMatch(req.TravelcardName)) throw new ArgumentException("Invalid travelcard name format");
    }
}