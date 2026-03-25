using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using TravelcardFunction.Models;

namespace TravelcardFunction.Helpers
{
    public class DatabaseHelper
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger _logger;
        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        public DatabaseHelper(string connectionString, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DatabaseHelper>();
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Connection string must be provided", nameof(connectionString));
            try
            {
                var builder = new NpgsqlDataSourceBuilder(connectionString);
                // Map enums to PostgreSQL enum types (preserve exact names)
                builder.MapEnum<TravelcardType>("travelcard_type_enum");
                builder.MapEnum<CardholderType>("cardholder_type_enum");
                _dataSource = builder.Build();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build NpgsqlDataSource");
                throw;
            }
        }

        public async Task<TravelcardDto?> GetTravelcardByNumberAsync(string travelcardNumber)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to
                FROM public.travelcards
                WHERE travelcard_number = @travelcard_number
                LIMIT 1;";
            var param = new NpgsqlParameter("travelcard_number", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = travelcardNumber };
            cmd.Parameters.Add(param);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            var travelcard = new TravelcardDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                TravelcardType = Enum.Parse<TravelcardType>(reader.GetString(reader.GetOrdinal("travelcard_type"))),
                TravelcardValidFrom = reader.GetFieldValue<DateTime>(reader.GetOrdinal("travelcard_valid_from")),
                TravelcardValidTo = reader.GetFieldValue<DateTime>(reader.GetOrdinal("travelcard_valid_to")),
                TravelcardName = reader.IsDBNull(reader.GetOrdinal("travelcard_name")) ? null : reader.GetString(reader.GetOrdinal("travelcard_name")),
                TravelcardNumber = reader.IsDBNull(reader.GetOrdinal("travelcard_number")) ? null : reader.GetString(reader.GetOrdinal("travelcard_number")),
                TravelcardRequestedDate = reader.GetFieldValue<DateTime>(reader.GetOrdinal("travelcard_requested_date")),
                TravelcardTransactionReference = reader.GetString(reader.GetOrdinal("travelcard_transaction_reference")),
                TravelcardUsableTo = reader.IsDBNull(reader.GetOrdinal("travelcard_usable_to")) ? null : reader.GetFieldValue<DateTime?>(reader.GetOrdinal("travelcard_usable_to"))
            };

            // Load cardholders
            travelcard.Cardholders = await GetCardholdersByTravelcardIdAsync(travelcard.Id);
            return travelcard;
        }

        private async Task<List<CardholderDto>> GetCardholdersByTravelcardIdAsync(int travelcardId)
        {
            var list = new List<CardholderDto>();
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key
                FROM public.cardholders
                WHERE travelcard_id = @travelcard_id;";
            var param = new NpgsqlParameter("travelcard_id", NpgsqlTypes.NpgsqlDbType.Integer) { Value = travelcardId };
            cmd.Parameters.Add(param);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var ch = new CardholderDto
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    TravelcardId = reader.GetInt32(reader.GetOrdinal("travelcard_id")),
                    CardholderTitle = reader.GetString(reader.GetOrdinal("cardholder_title")),
                    CardholderForename = reader.GetString(reader.GetOrdinal("cardholder_forename")),
                    CardholderSurname = reader.GetString(reader.GetOrdinal("cardholder_surname")),
                    CardholderType = Enum.Parse<CardholderType>(reader.GetString(reader.GetOrdinal("cardholder_type"))),
                    CardholderPhotoName = reader.GetString(reader.GetOrdinal("cardholder_photo_name")),
                    CardholderPhotoRRSKey = reader.IsDBNull(reader.GetOrdinal("cardholder_photo_rrs_key")) ? null : reader.GetString(reader.GetOrdinal("cardholder_photo_rrs_key")),
                    CardholderPhotoURL = reader.IsDBNull(reader.GetOrdinal("cardholder_photo_url")) ? null : reader.GetString(reader.GetOrdinal("cardholder_photo_url")),
                    CardholderPhotoKey = reader.IsDBNull(reader.GetOrdinal("cardholder_photo_key")) ? null : reader.GetString(reader.GetOrdinal("cardholder_photo_key"))
                };
                list.Add(ch);
            }
            return list;
        }
    }
}
