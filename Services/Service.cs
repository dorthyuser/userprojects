using System.Text.RegularExpressions;
using Npgsql;
using Npgsql.NameTranslation;
using Travelcardcsharplambda349Lambda.Models;
using Travelcardcsharplambda349Lambda.Models.Enums;

namespace Travelcardcsharplambda349Lambda.Services;

public interface ILoggerService
{
    void LogInfo(string message);
    void LogError(string message, Exception ex);
}

public sealed class ConsoleLogger : ILoggerService
{
    public void LogInfo(string message) => Console.WriteLine($"INFO: {message}");
    public void LogError(string message, Exception ex) => Console.WriteLine($"ERROR: {message} {ex}");
}

public sealed class Service
{
    private readonly ILoggerService _logger;
    private readonly NpgsqlDataSource _dataSource;

    public Service(ILoggerService logger)
    {
        _logger = logger;
        _dataSource = BuildDataSource();
    }

    public void LogInfo(string message) => _logger.LogInfo(message);
    public void LogError(string message, Exception ex) => _logger.LogError(message, ex);

    private static NpgsqlDataSource BuildDataSource()
    {
        var builder = new NpgsqlDataSourceBuilder(BuildConnectionString());
        builder.MapEnum<TravelcardTypeEnum>("travelcard_type_enum", nameTranslator: new NpgsqlNullNameTranslator());
        builder.MapEnum<CardholderTypeEnum>("cardholder_type_enum", nameTranslator: new NpgsqlNullNameTranslator());
        return builder.Build();
    }

    private static string BuildConnectionString()
    {
        var host = SecretsHelper.Get("host", "POSTGRESQLHOST");
        var port = SecretsHelper.Get("port", "POSTGRESQLPORT");
        var db = SecretsHelper.Get("dbname", "POSTGRESQLDATABASE");
        var user = SecretsHelper.Get("username", "POSTGRESQLUSERNAME");
        var pwd = SecretsHelper.Get("password", "POSTGRESQLPASSWORD");
        return $"Host={host};Port={port};Database={db};Username={user};Password={pwd};Pooling=true;";
    }

    public string? Validate(Request request)
    {
        if (request.TravelcardRequestedDate >= DateTime.UtcNow)
            return "travelcardRequestedDate must be in the past.";
        if (request.TravelcardValidFrom > request.TravelcardValidTo)
            return "travelcardValidFrom cannot be later than travelcardValidTo.";
        if (request.TravelcardValidTo <= DateTime.UtcNow)
            return "travelcardValidTo must be in the future.";
        if (request.TravelcardValidFrom > DateTime.UtcNow.AddMonths(1))
            return "travelcardValidFrom must not be later than one calendar month from today.";
        if (request.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen && request.TravelcardUsableTo is null)
            return "travelcardUsableTo is required for SixteenToSeventeen.";
        if (request.TravelcardUsableTo is not null && request.TravelcardUsableTo <= DateTime.UtcNow)
            return "travelcardUsableTo must be in the future.";
        if ((request.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen || request.TravelcardType == TravelcardTypeEnum.Veterans) && request.Cardholders.Any(c => c.CardholderType == CardholderTypeEnum.Secondary))
            return "Secondary cardholder is not allowed for SixteenToSeventeen or Veterans.";
        if (request.Cardholders.Count is < 1 or > 2)
            return "cardholders must contain exactly one or two items.";
        if (request.Cardholders.Count(c => c.CardholderType == CardholderTypeEnum.Primary) != 1)
            return "Exactly one Primary cardholder is required.";
        if (request.Cardholders.Count(c => c.CardholderType == CardholderTypeEnum.Secondary) > 1)
            return "Only one Secondary cardholder is allowed.";
        return null;
    }

    public async Task<Response> CreateAsync(Request request, string clientId, string? correlationId)
    {
        _logger.LogInfo($"Creating travelcard for client {clientId}, correlation {correlationId}");
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var tx = await connection.BeginTransactionAsync();
        try
        {
            var travelcardId = await InsertTravelcardAsync(connection, tx, request);
            foreach (var cardholder in request.Cardholders)
                await InsertCardholderAsync(connection, tx, travelcardId, cardholder);
            await tx.CommitAsync();
            return new Response { TravelcardId = travelcardId.ToString(), Token = GenerateToken() };
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static async Task<int> InsertTravelcardAsync(NpgsqlConnection connection, NpgsqlTransaction tx, Request request)
    {
        const string sql = @"INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to) VALUES (@travelcard_type, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to) RETURNING id;";
        await using var cmd = new NpgsqlCommand(sql, connection, tx);
        cmd.Parameters.AddWithValue("travelcard_type", request.TravelcardType);
        cmd.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
        cmd.Parameters.AddWithValue("travelcard_valid_to", request.TravelcardValidTo);
        cmd.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("travelcard_number", request.TravelcardNumber);
        cmd.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
        cmd.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference);
        cmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    private static async Task InsertCardholderAsync(NpgsqlConnection connection, NpgsqlTransaction tx, int travelcardId, CardholderRequest cardholder)
    {
        const string sql = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key) VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";
        await using var cmd = new NpgsqlCommand(sql, connection, tx);
        cmd.Parameters.AddWithValue("travelcard_id", travelcardId);
        cmd.Parameters.AddWithValue("cardholder_title", cardholder.CardholderTitle);
        cmd.Parameters.AddWithValue("cardholder_forename", cardholder.CardholderForename);
        cmd.Parameters.AddWithValue("cardholder_surname", cardholder.CardholderSurname);
        cmd.Parameters.AddWithValue("cardholder_type", cardholder.CardholderType);
        cmd.Parameters.AddWithValue("cardholder_photo_name", cardholder.CardholderPhotoName);
        cmd.Parameters.AddWithValue("cardholder_photo_rrs_key", (object?)cardholder.CardholderPhotoRRSKey ?? DBNull.Value);
        cmd.Parameters.AddWithValue("cardholder_photo_url", (object?)cardholder.CardholderPhotoURL ?? DBNull.Value);
        cmd.Parameters.AddWithValue("cardholder_photo_key", (object?)cardholder.CardholderPhotoKey ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    private static string GenerateToken() => Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
}