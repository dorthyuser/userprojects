using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using Microsoft.Extensions.Logging;
using TravelcardFunctionApp.Models;

namespace TravelcardFunctionApp.Helpers
{
    public class DbHelper
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger<DbHelper>? _logger;

        public DbHelper(ILogger<DbHelper>? logger = null)
        {
            _logger = logger;
            var connectionString = Environment.GetEnvironmentVariable("PostgresConnectionString") ?? "Host=localhost;Username=postgres;Password=postgres;Database=travelcards";
            var builder = new NpgsqlDataSourceBuilder(connectionString);
            // Map enums as required
            builder.MapEnum<TravelcardFunctionApp.Models.TravelcardType>("travelcard_type_enum");
            builder.MapEnum<TravelcardFunctionApp.Models.CardholderType>("cardholder_type_enum");
            _dataSource = builder.Build();
        }

        public async Task<(int travelcardId, string token)> InsertTravelcardAsync(TravelcardCreateRequest req)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                // Insert travelcard
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
INSERT INTO public.travelcards
(travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES
(@travelcardType::travelcard_type_enum, @travelcardValidFrom, @travelcardValidTo, @travelcardName, @travelcardNumber, @travelcardRequestedDate, @travelcardTransactionReference, @travelcardUsableTo)
RETURNING id;";

                    cmd.Parameters.AddWithValue("travelcardType", req.TravelcardType.ToString());
                    cmd.Parameters.AddWithValue("travelcardValidFrom", req.TravelcardValidFrom);
                    cmd.Parameters.AddWithValue("travelcardValidTo", req.TravelcardValidTo);
                    cmd.Parameters.AddWithValue("travelcardName", (object?)req.TravelcardName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("travelcardNumber", (object?)req.TravelcardNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("travelcardRequestedDate", req.TravelcardRequestedDate);
                    cmd.Parameters.AddWithValue("travelcardTransactionReference", (object?)req.TravelcardTransactionReference ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("travelcardUsableTo", req.TravelcardUsableTo.HasValue ? (object)req.TravelcardUsableTo.Value : DBNull.Value);

                    var result = await cmd.ExecuteScalarAsync();
                    var insertedId = Convert.ToInt32(result);

                    // Insert cardholders
                    foreach (var ch in req.Cardholders)
                    {
                        await using var cmdCh = conn.CreateCommand();
                        cmdCh.CommandText = @"
INSERT INTO public.cardholders
(travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES
(@travelcardId, @title, @forename, @surname, @type::cardholder_type_enum, @photoName, @photoRrsKey, @photoUrl, @photoKey);
";
                        cmdCh.Parameters.AddWithValue("travelcardId", insertedId);
                        cmdCh.Parameters.AddWithValue("title", ch.CardholderTitle);
                        cmdCh.Parameters.AddWithValue("forename", ch.CardholderForename);
                        cmdCh.Parameters.AddWithValue("surname", ch.CardholderSurname);
                        cmdCh.Parameters.AddWithValue("type", ch.CardholderType.ToString());
                        cmdCh.Parameters.AddWithValue("photoName", ch.CardholderPhotoName);
                        if (string.IsNullOrEmpty(ch.CardholderPhotoRrsKey))
                            cmdCh.Parameters.AddWithValue("photoRrsKey", DBNull.Value);
                        else
                            cmdCh.Parameters.AddWithValue("photoRrsKey", ch.CardholderPhotoRrsKey);
                        if (string.IsNullOrEmpty(ch.CardholderPhotoUrl))
                            cmdCh.Parameters.AddWithValue("photoUrl", DBNull.Value);
                        else
                            cmdCh.Parameters.AddWithValue("photoUrl", ch.CardholderPhotoUrl);
                        if (string.IsNullOrEmpty(ch.CardholderPhotoKey))
                            cmdCh.Parameters.AddWithValue("photoKey", DBNull.Value);
                        else
                            cmdCh.Parameters.AddWithValue("photoKey", ch.CardholderPhotoKey);

                        await cmdCh.ExecuteNonQueryAsync();
                    }

                    await tx.CommitAsync();
                    // Generate short token
                    var token = GenerateShortToken();
                    return (insertedId, token);
                }
            }
            catch
            {
                try { await tx.RollbackAsync(); } catch { }
                throw;
            }
            finally
            {
                try { await conn.CloseAsync(); } catch { }
            }
        }

        private static string GenerateShortToken()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var rng = new Random();
            var tokenChars = new char[6];
            for (int i = 0; i < tokenChars.Length; i++) tokenChars[i] = chars[rng.Next(chars.Length)];
            return new string(tokenChars);
        }
    }
}
