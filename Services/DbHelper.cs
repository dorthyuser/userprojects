using System.Text.RegularExpressions;
using azurecsharpfunction534.Models;
using Npgsql;
using NpgsqlTypes;

namespace azurecsharpfunction534.Services;

public class DbHelper
{
    private readonly NpgsqlDataSource _dataSource;

    public DbHelper()
    {
        var host = Environment.GetEnvironmentVariable("POSTGRESQLHOST");
        var port = Environment.GetEnvironmentVariable("POSTGRESQLPORT");
        var database = Environment.GetEnvironmentVariable("POSTGRESQLDATABASE");
        var username = Environment.GetEnvironmentVariable("POSTGRESQLUSERNAME");
        var password = Environment.GetEnvironmentVariable("POSTGRESQLPASSWORD");

        var builder = new NpgsqlDataSourceBuilder($"Host={host};Port={port};Database={database};Username={username};Password={password};Pooling=true;SSL Mode=Require;Trust Server Certificate=true");
        builder.MapEnum<TravelcardType>("travelcard_type_enum");
        builder.MapEnum<CardholderType>("cardholder_type_enum");
        _dataSource = builder.Build();
    }

    public async Task<CreateTravelcardResponse> CreateTravelcardAsync(CreateTravelcardRequest request)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES (@travelcard_type::travelcard_type_enum, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to)
RETURNING id;";
            cmd.Parameters.AddWithValue("travelcard_type", Enum.Parse<TravelcardType>(request.TravelcardType));
            cmd.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
            cmd.Parameters.AddWithValue("travelcard_valid_to", request.TravelcardValidTo);
            cmd.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("travelcard_number", request.TravelcardNumber);
            cmd.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
            cmd.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference);
            cmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);

            var travelcardId = (int)(await cmd.ExecuteScalarAsync() ?? throw new Exception("Failed to create travelcard."));

            foreach (var cardholder in request.Cardholders)
            {
                await using var chCmd = conn.CreateCommand();
                chCmd.Transaction = tx;
                chCmd.CommandText = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type::cardholder_type_enum, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";
                chCmd.Parameters.AddWithValue("travelcard_id", travelcardId);
                chCmd.Parameters.AddWithValue("cardholder_title", cardholder.CardholderTitle);
                chCmd.Parameters.AddWithValue("cardholder_forename", cardholder.CardholderForename);
                chCmd.Parameters.AddWithValue("cardholder_surname", cardholder.CardholderSurname);
                chCmd.Parameters.AddWithValue("cardholder_type", Enum.Parse<CardholderType>(cardholder.CardholderType));
                chCmd.Parameters.AddWithValue("cardholder_photo_name", cardholder.CardholderPhotoName);
                chCmd.Parameters.AddWithValue("cardholder_photo_rrs_key", (object?)cardholder.CardholderPhotoRRSKey ?? DBNull.Value);
                chCmd.Parameters.AddWithValue("cardholder_photo_url", (object?)cardholder.CardholderPhotoURL ?? DBNull.Value);
                chCmd.Parameters.AddWithValue("cardholder_photo_key", (object?)cardholder.CardholderPhotoKey ?? DBNull.Value);
                await chCmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return new CreateTravelcardResponse { TravelcardId = Guid.NewGuid().ToString(), Token = Random.Shared.Next(100000, 999999).ToString() };
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}

public enum TravelcardType
{
    Young,
    Barcklays,
    DevonandCornwall,
    TwoTogether,
    Family,
    Senior,
    DisabledPersons,
    Network,
    TwentySixToThirty,
    SixteenToSeventeen,
    Veterans
}

public enum CardholderType
{
    Primary,
    Secondary
}