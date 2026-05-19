using System;
using System.Threading.Tasks;
using Npgsql;
using TravelCardFunctionApp.Models;
using TravelCardFunctionApp.Services;

namespace TravelCardFunctionApp.Helpers;

public class DbHelper
{
    private readonly NpgsqlDataSource _dataSource;

    public DbHelper()
    {
        var host = SecretHelper.Get("POSTGRESQLHOST", "POSTGRESQLHOST");
        var port = SecretHelper.Get("POSTGRESQLPORT", "POSTGRESQLPORT");
        var database = SecretHelper.Get("POSTGRESQLDATABASE", "POSTGRESQLDATABASE");
        var username = SecretHelper.Get("POSTGRESQLUSERNAME", "POSTGRESQLUSERNAME");
        var password = SecretHelper.Get("POSTGRESQLPASSWORD", "POSTGRESQLPASSWORD");
        var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};Pooling=true;Maximum Pool Size=100;";
        var builder = new NpgsqlDataSourceBuilder(connectionString);
        _dataSource = builder.Build();
    }

    public async Task<CreateTravelcardResponse> CreateTravelcardAsync(CreateTravelcardRequest request, string correlationId)
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
            cmd.Parameters.AddWithValue("travelcard_type", request.TravelcardType.ToString());
            cmd.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
            cmd.Parameters.AddWithValue("travelcard_valid_to", request.TravelcardValidTo);
            cmd.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("travelcard_number", (object?)request.TravelcardNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
            cmd.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference ?? string.Empty);
            cmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);
            var travelcardId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            foreach (var cardholder in request.Cardholders)
            {
                await using var cardholderCmd = conn.CreateCommand();
                cardholderCmd.Transaction = tx;
                cardholderCmd.CommandText = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type::cardholder_type_enum, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";
                cardholderCmd.Parameters.AddWithValue("travelcard_id", travelcardId);
                cardholderCmd.Parameters.AddWithValue("cardholder_title", cardholder.CardholderTitle ?? string.Empty);
                cardholderCmd.Parameters.AddWithValue("cardholder_forename", cardholder.CardholderForename ?? string.Empty);
                cardholderCmd.Parameters.AddWithValue("cardholder_surname", cardholder.CardholderSurname ?? string.Empty);
                cardholderCmd.Parameters.AddWithValue("cardholder_type", cardholder.CardholderType.ToString());
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_name", cardholder.CardholderPhotoName ?? string.Empty);
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_rrs_key", (object?)cardholder.CardholderPhotoRRSKey ?? DBNull.Value);
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_url", (object?)cardholder.CardholderPhotoURL ?? DBNull.Value);
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_key", (object?)cardholder.CardholderPhotoKey ?? DBNull.Value);
                await cardholderCmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return new CreateTravelcardResponse { TravelcardId = Guid.NewGuid().ToString(), Token = GenerateToken() };
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static string GenerateToken()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var buffer = new char[6];
        for (var i = 0; i < buffer.Length; i++) buffer[i] = chars[random.Next(chars.Length)];
        return new string(buffer);
    }
}
