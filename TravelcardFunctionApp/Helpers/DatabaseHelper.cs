using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using TravelcardFunctionApp.Models;

namespace TravelcardFunctionApp.Helpers
{
    public class DatabaseHelper
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger<DatabaseHelper> _logger;
        public static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        public DatabaseHelper(IConfiguration configuration, ILogger<DatabaseHelper> logger)
        {
            _logger = logger;
            var conn = configuration.GetValue<string>("PostgresConnectionString") ?? "Host=localhost;Username=postgres;Password=postgres;Database=travelcardsdb";
            var builder = new NpgsqlDataSourceBuilder(conn);
            // Map enums to Postgres enum types preserving exact names
            builder.MapEnum<TravelcardType>("travelcard_type_enum", new ExactNameTranslator());
            builder.MapEnum<CardholderType>("cardholder_type_enum", new ExactNameTranslator());
            _dataSource = builder.Build();
        }

        public async Task<(int travelcardId, string token)> CreateTravelcardAsync(TravelcardRequest req)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                // Insert travelcard
                const string insertTravelcardSql = @"
INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES (@type::travelcard_type_enum, @validFrom, @validTo, @name, @number, @requestedDate, @transactionRef, @usableTo)
RETURNING id;";

                await using var cmd = new NpgsqlCommand(insertTravelcardSql, conn)
                {
                    Transaction = tx
                };

                cmd.Parameters.AddWithValue("type", req.TravelcardType.ToString());
                cmd.Parameters.AddWithValue("validFrom", req.TravelcardValidFrom);
                cmd.Parameters.AddWithValue("validTo", req.TravelcardValidTo);
                cmd.Parameters.AddWithValue("name", string.IsNullOrWhiteSpace(req.TravelcardName) ? (object)DBNull.Value : req.TravelcardName);
                cmd.Parameters.AddWithValue("number", req.TravelcardNumber);
                cmd.Parameters.AddWithValue("requestedDate", req.TravelcardRequestedDate);
                cmd.Parameters.AddWithValue("transactionRef", req.TravelcardTransactionReference);
                cmd.Parameters.AddWithValue("usableTo", req.TravelcardUsableTo.HasValue ? (object)req.TravelcardUsableTo.Value : DBNull.Value);

                var travelcardIdObj = await cmd.ExecuteScalarAsync();
                var travelcardId = Convert.ToInt32(travelcardIdObj);

                // Insert cardholders
                const string insertCardholderSql = @"
INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcardId, @title, @forename, @surname, @type::cardholder_type_enum, @photoName, @photoRrsKey, @photoUrl, @photoKey);
";

                foreach (var ch in req.Cardholders)
                {
                    await using var cmdCh = new NpgsqlCommand(insertCardholderSql, conn)
                    {
                        Transaction = tx
                    };
                    cmdCh.Parameters.AddWithValue("travelcardId", travelcardId);
                    cmdCh.Parameters.AddWithValue("title", ch.CardholderTitle);
                    cmdCh.Parameters.AddWithValue("forename", ch.CardholderForename);
                    cmdCh.Parameters.AddWithValue("surname", ch.CardholderSurname);
                    cmdCh.Parameters.AddWithValue("type", ch.CardholderType.ToString());
                    cmdCh.Parameters.AddWithValue("photoName", ch.CardholderPhotoName);
                    cmdCh.Parameters.AddWithValue("photoRrsKey", string.IsNullOrWhiteSpace(ch.CardholderPhotoRRSKey) ? (object)DBNull.Value : ch.CardholderPhotoRRSKey);
                    cmdCh.Parameters.AddWithValue("photoUrl", string.IsNullOrWhiteSpace(ch.CardholderPhotoURL) ? (object)DBNull.Value : ch.CardholderPhotoURL);
                    cmdCh.Parameters.AddWithValue("photoKey", string.IsNullOrWhiteSpace(ch.CardholderPhotoKey) ? (object)DBNull.Value : ch.CardholderPhotoKey);

                    await cmdCh.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                // Generate token (6 uppercase alphanumeric)
                var token = GenerateToken(6);
                _logger.LogInformation("Created travelcard id {TravelcardId}", travelcardId);
                return (travelcardId, token);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Database insert failed");
                throw;
            }
        }

        private static string GenerateToken(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var rng = new Random();
            var buffer = new char[length];
            for (var i = 0; i < length; i++) buffer[i] = chars[rng.Next(chars.Length)];
            return new string(buffer);
        }
    }
}
