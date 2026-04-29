using System;
using System.Threading.Tasks;
using Models;
using Npgsql;
using Services;

namespace Helpers
{
    public class DbHelper
    {
        private readonly NpgsqlDataSource _dataSource;

        public DbHelper()
        {
            string host = SecretHelper.Get("POSTGRESQL_HOST", "POSTGRESQL_HOST");
            string port = SecretHelper.Get("POSTGRESQL_PORT", "POSTGRESQL_PORT");
            string database = SecretHelper.Get("POSTGRESQL_DATABASE", "POSTGRESQL_DATABASE");
            string username = SecretHelper.Get("POSTGRESQL_USERNAME", "POSTGRESQL_USERNAME");
            string password = SecretHelper.Get("POSTGRESQL_PASSWORD", "POSTGRESQL_PASSWORD");
            string connectionString = SecretHelper.Get("PostgresConnectionString", "POSTGRES_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};Pooling=true;Maximum Pool Size=50";
            }

            NpgsqlDataSourceBuilder builder = new NpgsqlDataSourceBuilder(connectionString);
            builder.MapEnum<travelcardType_enum>("travelcard_type_enum");
            builder.MapEnum<cardholderType_enum>("cardholder_type_enum");
            _dataSource = builder.Build();
        }

        public async Task<TravelcardResponse> CreateTravelcardAsync(CreateTravelcardRequest request)
        {
            await using NpgsqlConnection conn = await _dataSource.OpenConnectionAsync();
            await using NpgsqlTransaction tx = await conn.BeginTransactionAsync();
            try
            {
                const string travelcardSql = @"INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES (@travelcard_type::travelcard_type_enum, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to)
RETURNING id;";

                await using NpgsqlCommand travelcardCmd = new NpgsqlCommand(travelcardSql, conn, tx);
                travelcardCmd.Parameters.AddWithValue("travelcard_type", request.TravelcardType.ToString());
                travelcardCmd.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
                travelcardCmd.Parameters.AddWithValue("travelcard_valid_to", request.TravelcardValidTo);
                travelcardCmd.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);
                travelcardCmd.Parameters.AddWithValue("travelcard_number", request.TravelcardNumber);
                travelcardCmd.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
                travelcardCmd.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference);
                travelcardCmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);

                object? insertedIdObj = await travelcardCmd.ExecuteScalarAsync();
                int travelcardId = Convert.ToInt32(insertedIdObj);
                string token = GenerateToken();

                const string cardholderSql = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type::cardholder_type_enum, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";

                foreach (CardholderRequest cardholder in request.Cardholders)
                {
                    await using NpgsqlCommand cardholderCmd = new NpgsqlCommand(cardholderSql, conn, tx);
                    cardholderCmd.Parameters.AddWithValue("travelcard_id", travelcardId);
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
                return new TravelcardResponse { TravelcardId = Guid.NewGuid().ToString(), Token = token };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private static string GenerateToken()
        {
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Span<char> buffer = stackalloc char[6];
            Random random = Random.Shared;
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = chars[random.Next(chars.Length)];
            }
            return new string(buffer);
        }
    }
}