using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using TravelcardFunction.Models;

namespace TravelcardFunction.Helpers
{
    public class PostgresDb
    {
        private readonly string _connectionString;
        private readonly NpgsqlDataSource _dataSource;

        public PostgresDb(string connectionString, ILogger<PostgresDb> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

            var builder = new NpgsqlDataSourceBuilder(_connectionString);
            // Map enums to postgres types
            try
            {
                builder.MapEnum<TravelcardType>("travelcard_type_enum");
                builder.MapEnum<CardholderType>("cardholder_type_enum");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to map enums - continuing without explicit mapping");
            }

            _dataSource = builder.Build();
        }

        public async Task<int> InsertTravelcardAsync(TravelcardRequest request, Microsoft.Extensions.Logging.ILogger logger)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                var insertTravelcard = @"INSERT INTO public.travelcards
(travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES
(@travelcardType::travelcard_type_enum, @travelcardValidFrom, @travelcardValidTo, @travelcardName, @travelcardNumber, @travelcardRequestedDate, @travelcardTransactionReference, @travelcardUsableTo)
RETURNING id;";

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = insertTravelcard;
                cmd.Transaction = tx;
                cmd.Parameters.AddWithValue("travelcardType", request.TravelcardType.ToString());
                cmd.Parameters.AddWithValue("travelcardValidFrom", request.TravelcardValidFrom);
                cmd.Parameters.AddWithValue("travelcardValidTo", request.TravelcardValidTo);
                cmd.Parameters.AddWithValue("travelcardName", (object?)request.TravelcardName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("travelcardNumber", (object?)request.TravelcardNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("travelcardRequestedDate", request.TravelcardRequestedDate);
                cmd.Parameters.AddWithValue("travelcardTransactionReference", request.TravelcardTransactionReference);
                cmd.Parameters.AddWithValue("travelcardUsableTo", (object?)request.TravelcardUsableTo ?? DBNull.Value);

                var idObj = await cmd.ExecuteScalarAsync();
                var travelcardId = Convert.ToInt32(idObj);

                // Insert cardholders
                foreach (var ch in request.Cardholders)
                {
                    var insertCardholder = @"INSERT INTO public.cardholders
(travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES
(@travelcardId, @cardholderTitle, @cardholderForename, @cardholderSurname, @cardholderType::cardholder_type_enum, @cardholderPhotoName, @cardholderPhotoRRSKey, @cardholderPhotoURL, @cardholderPhotoKey);";

                    await using var cmd2 = conn.CreateCommand();
                    cmd2.CommandText = insertCardholder;
                    cmd2.Transaction = tx;
                    cmd2.Parameters.AddWithValue("travelcardId", travelcardId);
                    cmd2.Parameters.AddWithValue("cardholderTitle", ch.CardholderTitle);
                    cmd2.Parameters.AddWithValue("cardholderForename", ch.CardholderForename);
                    cmd2.Parameters.AddWithValue("cardholderSurname", ch.CardholderSurname);
                    cmd2.Parameters.AddWithValue("cardholderType", ch.CardholderType.ToString());
                    cmd2.Parameters.AddWithValue("cardholderPhotoName", ch.CardholderPhotoName);
                    cmd2.Parameters.AddWithValue("cardholderPhotoRRSKey", (object?)ch.CardholderPhotoRRSKey ?? DBNull.Value);
                    cmd2.Parameters.AddWithValue("cardholderPhotoURL", (object?)ch.CardholderPhotoURL ?? DBNull.Value);
                    cmd2.Parameters.AddWithValue("cardholderPhotoKey", (object?)ch.CardholderPhotoKey ?? DBNull.Value);

                    await cmd2.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                logger.LogInformation("Inserted travelcard with id {Id}", travelcardId);
                return travelcardId;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                logger.LogError(ex, "Database insert failed");
                throw;
            }
        }
    }
}
