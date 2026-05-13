using System.Globalization;
using azurecsharppost1112.Models;
using Npgsql;
using NpgsqlTypes;

namespace azurecsharppost1112.Helpers;

public class DbHelper
{
    private readonly NpgsqlDataSource _dataSource;

    public DbHelper()
    {
        _dataSource = BuildDataSource();
    }

    private static NpgsqlDataSource BuildDataSource()
    {
        var host = Services.SecretHelper.Get("POSTGRESQLHOST", "POSTGRESQLHOST");
        var port = Services.SecretHelper.Get("POSTGRESQLPORT", "POSTGRESQLPORT");
        var database = Services.SecretHelper.Get("POSTGRESQLDATABASE", "POSTGRESQLDATABASE");
        var username = Services.SecretHelper.Get("POSTGRESQLUSERNAME", "POSTGRESQLUSERNAME");
        var password = Services.SecretHelper.Get("POSTGRESQLPASSWORD", "POSTGRESQLPASSWORD");

        var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};Pooling=true;Maximum Pool Size=50;Include Error Detail=true";
        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.MapEnum<travelcardType_enum>("travelcard_type_enum");
        builder.MapEnum<cardholderType_enum>("cardholder_type_enum");
        return builder.Build();
    }

    public async Task<DbCreateTravelcardResult> CreateTravelcardAsync(CreateTravelcardRequest request)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var travelcardUuid = Guid.NewGuid();
            var token = GenerateToken();

            await using (var cmd = new NpgsqlCommand(@"INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to) VALUES (@travelcard_type::travelcard_type_enum, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to) RETURNING id;", conn, tx))
            {
                cmd.Parameters.AddWithValue("travelcard_type", request.travelcardType.ToString());
                cmd.Parameters.AddWithValue("travelcard_valid_from", NpgsqlDbType.TimestampTz, request.travelcardValidFrom.ToUniversalTime());
                cmd.Parameters.AddWithValue("travelcard_valid_to", NpgsqlDbType.TimestampTz, request.travelcardValidTo.ToUniversalTime());
                cmd.Parameters.AddWithValue("travelcard_name", (object?)request.travelcardName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("travelcard_number", request.travelcardNumber);
                cmd.Parameters.AddWithValue("travelcard_requested_date", NpgsqlDbType.TimestampTz, request.travelcardRequestedDate.ToUniversalTime());
                cmd.Parameters.AddWithValue("travelcard_transaction_reference", request.travelcardTransactionReference);
                cmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.travelcardUsableTo?.ToUniversalTime() ?? DBNull.Value);
                var travelcardId = Convert.ToInt32(await cmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture);

                foreach (var cardholder in request.cardholders)
                {
                    await using var cardholderCmd = new NpgsqlCommand(@"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key) VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type::cardholder_type_enum, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);", conn, tx);
                    cardholderCmd.Parameters.AddWithValue("travelcard_id", travelcardId);
                    cardholderCmd.Parameters.AddWithValue("cardholder_title", cardholder.cardholderTitle);
                    cardholderCmd.Parameters.AddWithValue("cardholder_forename", cardholder.cardholderForename);
                    cardholderCmd.Parameters.AddWithValue("cardholder_surname", cardholder.cardholderSurname);
                    cardholderCmd.Parameters.AddWithValue("cardholder_type", cardholder.cardholderType.ToString());
                    cardholderCmd.Parameters.AddWithValue("cardholder_photo_name", cardholder.cardholderPhotoName);
                    cardholderCmd.Parameters.AddWithValue("cardholder_photo_rrs_key", (object?)cardholder.cardholderPhotoRRSKey ?? DBNull.Value);
                    cardholderCmd.Parameters.AddWithValue("cardholder_photo_url", (object?)cardholder.cardholderPhotoURL ?? DBNull.Value);
                    cardholderCmd.Parameters.AddWithValue("cardholder_photo_key", (object?)cardholder.cardholderPhotoKey ?? DBNull.Value);
                    await cardholderCmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return new DbCreateTravelcardResult { TravelcardId = travelcardUuid.ToString(), Token = token };
            }
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static string GenerateToken()
    {
        var bytes = Guid.NewGuid().ToByteArray();
        return Convert.ToBase64String(bytes).Replace("=", string.Empty).Replace("+", string.Empty).Replace("/", string.Empty)[..6].ToUpperInvariant();
    }
}
