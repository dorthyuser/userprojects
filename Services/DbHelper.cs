using System;
using System.Threading.Tasks;
using Npgsql;
using TravelcardFunctionApp.Models;

namespace TravelcardFunctionApp.Services;

public sealed class TravelcardDbHelper
{
    private readonly NpgsqlDataSource _dataSource;

    public TravelcardDbHelper()
    {
        var host = SecretHelper.Get("POSTGRESQLHOST", "POSTGRESQLHOST");
        var port = SecretHelper.Get("POSTGRESQLPORT", "POSTGRESQLPORT");
        var database = SecretHelper.Get("POSTGRESQLDATABASE", "POSTGRESQLDATABASE");
        var username = SecretHelper.Get("POSTGRESQLUSERNAME", "POSTGRESQLUSERNAME");
        var password = SecretHelper.Get("POSTGRESQLPASSWORD", "POSTGRESQLPASSWORD");

        var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};Pooling=true;Maximum Pool Size=20;";
        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.MapEnum<TravelcardTypeEnum>("travelcard_type_enum");
        builder.MapEnum<CardholderTypeEnum>("cardholder_type_enum");
        _dataSource = builder.Build();
    }

    public async Task<CreateTravelcardResult> CreateTravelcardAsync(CreateTravelcardRequest request)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            const string sql = @"INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES (@travelcard_type::travelcard_type_enum, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to)
RETURNING id;";
            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("travelcard_type", request.TravelcardType.ToString());
            cmd.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
            cmd.Parameters.AddWithValue("travelcard_valid_to", request.TravelcardValidTo);
            cmd.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("travelcard_number", request.TravelcardNumber);
            cmd.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
            cmd.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference);
            cmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);

            var travelcardIdObj = await cmd.ExecuteScalarAsync();
            var travelcardId = Convert.ToInt32(travelcardIdObj).ToString();
            foreach (var cardholder in request.Cardholders)
            {
                const string cardholderSql = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type::cardholder_type_enum, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";
                await using var cardholderCmd = new NpgsqlCommand(cardholderSql, conn, tx);
                cardholderCmd.Parameters.AddWithValue("travelcard_id", Convert.ToInt32(travelcardIdObj));
                cardholderCmd.Parameters.AddWithValue("cardholder_title", cardholder.CardholderTitle);
                cardholderCmd.Parameters.AddWithValue("cardholder_forename", cardholder.CardholderForename);
                cardholderCmd.Parameters.AddWithValue("cardholder_surname", cardholder.CardholderSurname);
                cardholderCmd.Parameters.AddWithValue("cardholder_type", cardholder.CardholderType.ToString());
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_name", cardholder.CardholderPhotoName);
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_rrs_key", (object?)cardholder.CardholderPhotoRRSKey ?? DBNull.Value);
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_url", (object?)cardholder.CardholderPhotoURL ?? DBNull.Value);
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_key", (object?)cardholder.CardholderPhotoKey ?? DBNull.Value);
                await cardholderCmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return new CreateTravelcardResult { TravelcardId = Guid.NewGuid().ToString(), Token = GenerateToken() };
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static string GenerateToken()
    {
        return "P5SSY6";
    }
}