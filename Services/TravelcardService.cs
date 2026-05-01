using azuretravelcardapi1218.Models;
using Npgsql;
using Npgsql.NameTranslation;

namespace azuretravelcardapi1218.Services;

public sealed class TravelcardService : ITravelcardService
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

    public async Task<TravelcardCreateResponse> CreateAsync(TravelcardCreateRequest request, string? correlationId, CancellationToken cancellationToken)
    {
        var token = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var travelcardId = Guid.NewGuid();

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to) VALUES (@type::travelcard_type_enum, @valid_from, @valid_to, @name, @number, @requested_date, @transaction_reference, @usable_to) RETURNING id";
            cmd.Parameters.AddWithValue("type", request.TravelcardType.ToString());
            cmd.Parameters.AddWithValue("valid_from", request.TravelcardValidFrom);
            cmd.Parameters.AddWithValue("valid_to", request.TravelcardValidTo);
            cmd.Parameters.AddWithValue("name", (object?)request.TravelcardName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("number", request.TravelcardNumber);
            cmd.Parameters.AddWithValue("requested_date", request.TravelcardRequestedDate);
            cmd.Parameters.AddWithValue("transaction_reference", request.TravelcardTransactionReference);
            cmd.Parameters.AddWithValue("usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);
            var insertedId = (int)(await cmd.ExecuteScalarAsync(cancellationToken) ?? 0);

            foreach (var cardholder in request.Cardholders)
            {
                await using var chCmd = conn.CreateCommand();
                chCmd.Transaction = tx;
                chCmd.CommandText = "INSERT INTO cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key) VALUES (@travelcard_id, @title, @forename, @surname, @type::cardholder_type_enum, @photo_name, @rrs_key, @photo_url, @photo_key)";
                chCmd.Parameters.AddWithValue("travelcard_id", insertedId);
                chCmd.Parameters.AddWithValue("title", cardholder.CardholderTitle);
                chCmd.Parameters.AddWithValue("forename", cardholder.CardholderForename);
                chCmd.Parameters.AddWithValue("surname", cardholder.CardholderSurname);
                chCmd.Parameters.AddWithValue("type", cardholder.CardholderType.ToString());
                chCmd.Parameters.AddWithValue("photo_name", cardholder.CardholderPhotoName);
                chCmd.Parameters.AddWithValue("rrs_key", (object?)cardholder.CardholderPhotoRRSKey ?? DBNull.Value);
                chCmd.Parameters.AddWithValue("photo_url", (object?)cardholder.CardholderPhotoURL ?? DBNull.Value);
                chCmd.Parameters.AddWithValue("photo_key", (object?)cardholder.CardholderPhotoKey ?? DBNull.Value);
                await chCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            return new TravelcardCreateResponse { TravelcardId = travelcardId, Token = token };
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}