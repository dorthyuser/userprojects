using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using travelcardlambdachsarp549.Models;

namespace travelcardlambdachsarp549.Helpers
{
    public class DbHelper
    {
        private readonly NpgsqlDataSource _dataSource;

        public DbHelper()
        {
            string host = SecretHelper.Get("POSTGRESQLHOST", "POSTGRESQL_HOST");
            string port = SecretHelper.Get("POSTGRESQLPORT", "POSTGRESQL_PORT");
            string database = SecretHelper.Get("POSTGRESQLDATABASE", "POSTGRESQL_DATABASE");
            string username = SecretHelper.Get("POSTGRESQLUSERNAME", "POSTGRESQL_USERNAME");
            string password = SecretHelper.Get("POSTGRESQLPASSWORD", "POSTGRESQL_PASSWORD");

            string connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};Pooling=true;Minimum Pool Size=0;Maximum Pool Size=50;Timeout=15;Command Timeout=30";
            var builder = new NpgsqlDataSourceBuilder(connectionString);
            builder.MapEnum<travelcard_type_enum>("travelcard_type_enum");
            builder.MapEnum<cardholder_type_enum>("cardholder_type_enum");
            _dataSource = builder.Build();
        }

        public async Task<CreateTravelcardResult> CreateTravelcardAsync(CreateTravelcardRequest request)
        {
            await using NpgsqlConnection conn = await _dataSource.OpenConnectionAsync();
            await using NpgsqlTransaction tx = await conn.BeginTransactionAsync();
            try
            {
                long travelcardDbId;
                const string travelcardSql = @"INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES (@travelcard_type::travelcard_type_enum, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to)
RETURNING id;";

                await using (var cmd = new NpgsqlCommand(travelcardSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("travelcard_type", request.TravelcardType.ToString());
                    cmd.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
                    cmd.Parameters.AddWithValue("travelcard_valid_to", request.TravelcardValidTo);
                    cmd.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("travelcard_number", request.TravelcardNumber);
                    cmd.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
                    cmd.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference);
                    cmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);
                    travelcardDbId = (long)(await cmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("Failed to insert travelcard."));
                }

                foreach (CardholderRequest cardholder in request.Cardholders)
                {
                    const string cardholderSql = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type::cardholder_type_enum, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";

                    await using var cmd = new NpgsqlCommand(cardholderSql, conn, tx);
                    cmd.Parameters.AddWithValue("travelcard_id", (int)travelcardDbId);
                    cmd.Parameters.AddWithValue("cardholder_title", cardholder.CardholderTitle);
                    cmd.Parameters.AddWithValue("cardholder_forename", cardholder.CardholderForename);
                    cmd.Parameters.AddWithValue("cardholder_surname", cardholder.CardholderSurname);
                    cmd.Parameters.AddWithValue("cardholder_type", cardholder.CardholderType.ToString());
                    cmd.Parameters.AddWithValue("cardholder_photo_name", cardholder.CardholderPhotoName);
                    cmd.Parameters.AddWithValue("cardholder_photo_rrs_key", (object?)cardholder.CardholderPhotoRRSKey ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("cardholder_photo_url", (object?)cardholder.CardholderPhotoURL ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("cardholder_photo_key", (object?)cardholder.CardholderPhotoKey ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }

                string token = GenerateToken();
                await tx.CommitAsync();
                return new CreateTravelcardResult { TravelcardId = Guid.NewGuid().ToString(), Token = token };
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
            Span<char> buffer = stackalloc char[6];
            var random = Random.Shared;
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = chars[random.Next(chars.Length)];
            }
            return new string(buffer);
        }
    }
}