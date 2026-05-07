using Npgsql;
using Npgsql.NameTranslation;
using Microsoft.Extensions.Logging;
using Travelcardchsarplambda1050Lambda.Models;

namespace Travelcardchsarplambda1050Lambda.Services;

public sealed class Service
{
    private readonly ILogger _logger;
    private static readonly object _lock = new();
    private static NpgsqlDataSource? _dataSource;

    public Service(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<CreateTravelcardResponse> CreateTravelcardAsync(CreateTravelcardRequest request, string? clientId, string awsRequestId)
    {
        var connectionString = BuildConnectionString();
        var dataSource = GetOrCreateDataSource(connectionString);
        var travelcardId = Guid.NewGuid().ToString();
        var token = GenerateToken();

        _logger.LogInformation("DB operation: travelcards INSERT");
        _logger.LogInformation("DB operation: cardholders INSERT");

        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            await InsertTravelcardAsync(connection, transaction, request);
            await InsertCardholdersAsync(connection, transaction, request);
            await transaction.CommitAsync();
            _logger.LogInformation("DB success: travelcards generated ID {Id}", travelcardId);
            _logger.LogInformation("Response generated ID: {Id}", travelcardId);
            return new CreateTravelcardResponse { TravelcardId = travelcardId, Token = token };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Database operation failed in CreateTravelcardAsync.");
            throw;
        }
    }

    private static string BuildConnectionString()
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = SecretsHelper.Get("host", "POSTGRESQLHOST"),
            Port = int.Parse(SecretsHelper.Get("port", "POSTGRESQLPORT")),
            Database = SecretsHelper.Get("dbname", "POSTGRESQLDATABASE"),
            Username = SecretsHelper.Get("username", "POSTGRESQLUSERNAME"),
            Password = SecretsHelper.Get("password", "POSTGRESQLPASSWORD")
        };
        return builder.ConnectionString;
    }

    private static NpgsqlDataSource GetOrCreateDataSource(string connectionString)
    {
        if (_dataSource is not null) return _dataSource;
        lock (_lock)
        {
            if (_dataSource is null)
            {
                var builder = new NpgsqlDataSourceBuilder(connectionString);
                builder.MapEnum<TravelcardTypeEnum>("travelcard_type_enum", new NpgsqlNullNameTranslator());
                builder.MapEnum<CardholderTypeEnum>("cardholder_type_enum", new NpgsqlNullNameTranslator());
                _dataSource = builder.Build();
            }
        }
        return _dataSource!;
    }

    private async Task InsertTravelcardAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CreateTravelcardRequest request)
    {
        const string sql = @"INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to) VALUES (@travelcard_type, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to);";
        await using var cmd = new NpgsqlCommand(sql, connection, transaction);
        cmd.Parameters.Add(new NpgsqlParameter("travelcard_type", request.TravelcardType));
        cmd.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
        cmd.Parameters.AddWithValue("travelcard_valid_to", request.TravelcardValidTo);
        cmd.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("travelcard_number", request.TravelcardNumber);
        cmd.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
        cmd.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference);
        cmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task InsertCardholdersAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CreateTravelcardRequest request)
    {
        const string sql = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key) VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";
        foreach (var cardholder in request.Cardholders)
        {
            await using var cmd = new NpgsqlCommand(sql, connection, transaction);
            cmd.Parameters.AddWithValue("travelcard_id", 0);
            cmd.Parameters.AddWithValue("cardholder_title", cardholder.CardholderTitle);
            cmd.Parameters.AddWithValue("cardholder_forename", cardholder.CardholderForename);
            cmd.Parameters.AddWithValue("cardholder_surname", cardholder.CardholderSurname);
            cmd.Parameters.Add(new NpgsqlParameter("cardholder_type", cardholder.CardholderType));
            cmd.Parameters.AddWithValue("cardholder_photo_name", cardholder.CardholderPhotoName);
            cmd.Parameters.AddWithValue("cardholder_photo_rrs_key", (object?)cardholder.CardholderPhotoRRSKey ?? DBNull.Value);
            cmd.Parameters.AddWithValue("cardholder_photo_url", (object?)cardholder.CardholderPhotoURL ?? DBNull.Value);
            cmd.Parameters.AddWithValue("cardholder_photo_key", (object?)cardholder.CardholderPhotoKey ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static string GenerateToken() => Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
}