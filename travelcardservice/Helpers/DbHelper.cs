using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Npgsql.NameTranslation;
using NpgsqlTypes;
using Npgsql.TypeMapping;
using Npgsql.LegacyPostgis; 
using travelcardservice.Models;

namespace travelcardservice.Helpers
{
    public class DbHelper
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger<DbHelper> _logger;
        public static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        public DbHelper(IConfiguration config, ILogger<DbHelper> logger)
        {
            _logger = logger;
            var connectionString = config["PostgresConnectionString"] ?? "Host=localhost;Username=postgres;Password=postgres;Database=travelcards;Pooling=true";
            _logger.LogInformation("Initializing NpgsqlDataSourceBuilder and mapping enums");
            var builder = new NpgsqlDataSourceBuilder(connectionString);
            builder.MapEnum<TravelcardType>("travelcard_type_enum");
            builder.MapEnum<CardholderType>("cardholder_type_enum");
            _dataSource = builder.Build();
        }

        public async Task<(int TravelcardId, string Token)> CreateTravelcardAsync(TravelcardRequest req)
        {
            _logger.LogInformation("DbHelper.CreateTravelcardAsync - start");
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                // Insert travelcard
                var insertTravelcardSql = @"
INSERT INTO travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES (@travelcard_type::travelcard_type_enum, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to)
RETURNING id;";

                await using var cmd = new NpgsqlCommand(insertTravelcardSql, conn, tx);
                cmd.Parameters.AddWithValue("@travelcard_type", req.TravelcardType.ToString());
                cmd.Parameters.AddWithValue("@travelcard_valid_from", req.TravelcardValidFrom);
                cmd.Parameters.AddWithValue("@travelcard_valid_to", req.TravelcardValidTo);
                cmd.Parameters.AddWithValue("@travelcard_name", string.IsNullOrWhiteSpace(req.TravelcardName) ? (object)DBNull.Value : req.TravelcardName);
                cmd.Parameters.AddWithValue("@travelcard_number", req.TravelcardNumber);
                cmd.Parameters.AddWithValue("@travelcard_requested_date", req.TravelcardRequestedDate);
                cmd.Parameters.AddWithValue("@travelcard_transaction_reference", req.TravelcardTransactionReference);
                cmd.Parameters.AddWithValue("@travelcard_usable_to", req.TravelcardUsableTo.HasValue ? (object)req.TravelcardUsableTo.Value : DBNull.Value);

                var travelcardIdObj = await cmd.ExecuteScalarAsync();
                if (travelcardIdObj == null)
                {
                    throw new Exception("Failed to insert travelcard");
                }

                var travelcardId = Convert.ToInt32(travelcardIdObj);

                // Insert cardholders
                var insertCardholderSql = @"
INSERT INTO cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type::cardholder_type_enum, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);
";

                foreach (var ch in req.Cardholders)
                {
                    await using var chCmd = new NpgsqlCommand(insertCardholderSql, conn, tx);
                    chCmd.Parameters.AddWithValue("@travelcard_id", travelcardId);
                    chCmd.Parameters.AddWithValue("@cardholder_title", ch.CardholderTitle);
                    chCmd.Parameters.AddWithValue("@cardholder_forename", ch.CardholderForename);
                    chCmd.Parameters.AddWithValue("@cardholder_surname", ch.CardholderSurname);
                    chCmd.Parameters.AddWithValue("@cardholder_type", ch.CardholderType.ToString());
                    chCmd.Parameters.AddWithValue("@cardholder_photo_name", ch.CardholderPhotoName);
                    chCmd.Parameters.AddWithValue("@cardholder_photo_rrs_key", string.IsNullOrWhiteSpace(ch.CardholderPhotoRRSKey) ? (object)DBNull.Value : ch.CardholderPhotoRRSKey);
                    chCmd.Parameters.AddWithValue("@cardholder_photo_url", string.IsNullOrWhiteSpace(ch.CardholderPhotoURL) ? (object)DBNull.Value : ch.CardholderPhotoURL);
                    chCmd.Parameters.AddWithValue("@cardholder_photo_key", string.IsNullOrWhiteSpace(ch.CardholderPhotoKey) ? (object)DBNull.Value : ch.CardholderPhotoKey);

                    await chCmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                var token = TokenGenerator.GenerateToken(6);

                _logger.LogInformation("DbHelper.CreateTravelcardAsync - success id {Id}", travelcardId);
                return (travelcardId, token);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "DbHelper.CreateTravelcardAsync - error");
                throw;
            }
            finally
            {
                _logger.LogInformation("DbHelper.CreateTravelcardAsync - exit");
            }
        }
    }
}
