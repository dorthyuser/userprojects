using System;
using System.Threading.Tasks;
using azuretravelcardfunction309.Models;
using azuretravelcardfunction309.Services;
using Npgsql;

namespace azuretravelcardfunction309.Helpers;

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

        var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};Pooling=true;Maximum Pool Size=20;Include Error Detail=true";
        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.MapEnum<travelcardType_enum>("travelcard_type_enum");
        builder.MapEnum<cardholderType_enum>("cardholder_type_enum");
        _dataSource = builder.Build();
    }

    public async Task<CreateTravelcardResponse> CreateTravelcardAsync(CreateTravelcardRequest request)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();

        try
        {
            var travelcardId = Guid.NewGuid().ToString();
            var token = RandomTokenGenerator.Generate(6);

            int insertedTravelcardDbId;
            await using (var cmd = new NpgsqlCommand(@"INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to) VALUES (@travelcard_type::travelcard_type_enum, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to) RETURNING id;", conn, tx))
            {
                cmd.Parameters.AddWithValue("travelcard_type", request.travelcardType.ToString());
                cmd.Parameters.AddWithValue("travelcard_valid_from", request.travelcardValidFrom);
                cmd.Parameters.AddWithValue("travelcard_valid_to", request.travelcardValidTo);
                cmd.Parameters.AddWithValue("travelcard_name", (object?)request.travelcardName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("travelcard_number", request.travelcardNumber);
                cmd.Parameters.AddWithValue("travelcard_requested_date", request.travelcardRequestedDate);
                cmd.Parameters.AddWithValue("travelcard_transaction_reference", request.travelcardTransactionReference);
                cmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.travelcardUsableTo ?? DBNull.Value);
                insertedTravelcardDbId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            foreach (var cardholder in request.cardholders)
            {
                await using var cmd = new NpgsqlCommand(@"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key) VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type::cardholder_type_enum, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);", conn, tx);
                cmd.Parameters.AddWithValue("travelcard_id", insertedTravelcardDbId);
                cmd.Parameters.AddWithValue("cardholder_title", cardholder.cardholderTitle);
                cmd.Parameters.AddWithValue("cardholder_forename", cardholder.cardholderForename);
                cmd.Parameters.AddWithValue("cardholder_surname", cardholder.cardholderSurname);
                cmd.Parameters.AddWithValue("cardholder_type", cardholder.cardholderType.ToString());
                cmd.Parameters.AddWithValue("cardholder_photo_name", cardholder.cardholderPhotoName);
                cmd.Parameters.AddWithValue("cardholder_photo_rrs_key", (object?)cardholder.cardholderPhotoRRSKey ?? DBNull.Value);
                cmd.Parameters.AddWithValue("cardholder_photo_url", (object?)cardholder.cardholderPhotoURL ?? DBNull.Value);
                cmd.Parameters.AddWithValue("cardholder_photo_key", (object?)cardholder.cardholderPhotoKey ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return new CreateTravelcardResponse(travelcardId, token);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
