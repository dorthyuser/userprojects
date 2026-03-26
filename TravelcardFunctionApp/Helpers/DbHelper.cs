using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql.Replication.PgOutput.Messages;
using TravelcardFunctionApp.Models;
using Enums = TravelcardFunctionApp.Enums;

namespace TravelcardFunctionApp.Helpers
{
    public class DbHelper
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger<DbHelper> _logger;

        public DbHelper(IConfiguration config, ILogger<DbHelper> logger)
        {
            _logger = logger;
            var connectionString = config["PostgresConnectionString"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogError("PostgresConnectionString is not configured");
                throw new InvalidOperationException("PostgresConnectionString is not configured");
            }

            var builder = new NpgsqlDataSourceBuilder(connectionString);
            builder.MapEnum<Enums.TravelcardType>("travelcard_type_enum");
            builder.MapEnum<Enums.CardholderType>("cardholder_type_enum");
            _dataSource = builder.Build();
        }

        public async Task<(int id, string token)> InsertTravelcardAsync(TravelcardRequest payload, CancellationToken cancellationToken)
        {
            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var tx = await conn.BeginTransactionAsync(cancellationToken);
            try
            {
                // Insert travelcard
                var insertTravelcardSql = @"INSERT INTO public.travelcards
(travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES (@travelcardType::travelcard_type_enum, @validFrom, @validTo, @name, @number, @requestedDate, @transactionReference, @usableTo)
RETURNING id;";

                await using var cmd = new NpgsqlCommand(insertTravelcardSql, conn, tx);
                cmd.Parameters.AddWithValue("travelcardType", payload.TravelcardType.ToString());
                cmd.Parameters.AddWithValue("validFrom", payload.TravelcardValidFrom);
                cmd.Parameters.AddWithValue("validTo", payload.TravelcardValidTo);
                cmd.Parameters.AddWithValue("name", (object?)payload.TravelcardName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("number", payload.TravelcardNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("requestedDate", payload.TravelcardRequestedDate);
                cmd.Parameters.AddWithValue("transactionReference", payload.TravelcardTransactionReference ?? string.Empty);
                cmd.Parameters.AddWithValue("usableTo", (object?)payload.TravelcardUsableTo ?? DBNull.Value);

                var idObj = await cmd.ExecuteScalarAsync(cancellationToken);
                var travelcardDbId = Convert.ToInt32(idObj);

                // Insert cardholders
                var insertCardholderSql = @"INSERT INTO public.cardholders
(travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcardId, @title, @forename, @surname, @type::cardholder_type_enum, @photoName, @rrsKey, @url, @key);";

                foreach (var ch in payload.Cardholders)
                {
                    await using var cmdCh = new NpgsqlCommand(insertCardholderSql, conn, tx);
                    cmdCh.Parameters.AddWithValue("travelcardId", travelcardDbId);
                    cmdCh.Parameters.AddWithValue("title", ch.CardholderTitle ?? string.Empty);
                    cmdCh.Parameters.AddWithValue("forename", ch.CardholderForename ?? string.Empty);
                    cmdCh.Parameters.AddWithValue("surname", ch.CardholderSurname ?? string.Empty);
                    cmdCh.Parameters.AddWithValue("type", ch.CardholderType.ToString());
                    cmdCh.Parameters.AddWithValue("photoName", ch.CardholderPhotoName ?? string.Empty);
                    cmdCh.Parameters.AddWithValue("rrsKey", (object?)ch.CardholderPhotoRRSKey ?? DBNull.Value);
                    cmdCh.Parameters.AddWithValue("url", (object?)ch.CardholderPhotoURL ?? DBNull.Value);
                    cmdCh.Parameters.AddWithValue("key", (object?)ch.CardholderPhotoKey ?? DBNull.Value);
                    await cmdCh.ExecuteNonQueryAsync(cancellationToken);
                }

                await tx.CommitAsync(cancellationToken);

                var token = GenerateToken(6);
                _logger.LogInformation("Inserted travelcard with id {Id}", travelcardDbId);
                return (travelcardDbId, token);
            }
            catch (Exception ex)
            {
                try
                {
                    await tx.RollbackAsync(cancellationToken);
                }
                catch (Exception rollEx)
                {
                    _logger.LogError(rollEx, "Rollback failed");
                }

                _logger.LogError(ex, "Error inserting travelcard");
                throw;
            }
        }

        private static string GenerateToken(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
            var bytes = new byte[length];
            rng.GetBytes(bytes);
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[bytes[i] % chars.Length];
            }
            return new string(result);
        }
    }
}
