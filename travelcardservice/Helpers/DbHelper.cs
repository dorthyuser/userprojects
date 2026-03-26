using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using travelcardservice.Models;

namespace travelcardservice.Helpers
{
    public class DbHelper
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger<DbHelper> _logger;

        public DbHelper(IConfiguration configuration, ILogger<DbHelper> logger)
        {
            _logger = logger;
            string conn = configuration["PostgresConnectionString"] ?? "Host=localhost;Username=postgres;Password=postgres;Database=travelcards;Pooling=true";
            var builder = new NpgsqlDataSourceBuilder(conn);
            // Register enums with exact name translator (extension provided)
            builder.MapEnum<TravelcardType>("travelcard_type_enum", new ExactNameTranslator());
            builder.MapEnum<CardholderType>("cardholder_type_enum", new ExactNameTranslator());
            _dataSource = builder.Build();
        }

        public async Task<int> InsertTravelcardAsync(TravelcardRequest travelcard)
        {
            const string sql = @"INSERT INTO travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to) VALUES (@type::travelcard_type_enum, @validFrom, @validTo, @name, @number, @requested, @txref, @usable) RETURNING id;";
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("type", travelcard.TravelcardType.ToString());
            cmd.Parameters.AddWithValue("validFrom", travelcard.TravelcardValidFrom);
            cmd.Parameters.AddWithValue("validTo", travelcard.TravelcardValidTo);
            cmd.Parameters.AddWithValue("name", (object) (string.IsNullOrEmpty(travelcard.TravelcardName) ? DBNull.Value : travelcard.TravelcardName));
            cmd.Parameters.AddWithValue("number", travelcard.TravelcardNumber);
            cmd.Parameters.AddWithValue("requested", travelcard.TravelcardRequestedDate);
            cmd.Parameters.AddWithValue("txref", travelcard.TravelcardTransactionReference);
            cmd.Parameters.AddWithValue("usable", (object) (travelcard.TravelcardUsableTo.HasValue ? travelcard.TravelcardUsableTo.Value : DBNull.Value));

            try
            {
                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting travelcard");
                throw;
            }
        }

        public async Task InsertCardholdersAsync(int travelcardId, List<CardholderDto> cardholders)
        {
            const string sql = @"INSERT INTO cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key) VALUES (@travelcardId, @title, @forename, @surname, @type::cardholder_type_enum, @photoName, @rrsKey, @url, @key) RETURNING id;";
            await using var conn = await _dataSource.OpenConnectionAsync();
            foreach (var ch in cardholders)
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("travelcardId", travelcardId);
                cmd.Parameters.AddWithValue("title", ch.CardholderTitle);
                cmd.Parameters.AddWithValue("forename", ch.CardholderForename);
                cmd.Parameters.AddWithValue("surname", ch.CardholderSurname);
                cmd.Parameters.AddWithValue("type", ch.CardholderType.ToString());
                cmd.Parameters.AddWithValue("photoName", ch.CardholderPhotoName);
                cmd.Parameters.AddWithValue("rrsKey", (object) (string.IsNullOrEmpty(ch.CardholderPhotoRRSKey) ? DBNull.Value : ch.CardholderPhotoRRSKey));
                cmd.Parameters.AddWithValue("url", (object) (string.IsNullOrEmpty(ch.CardholderPhotoURL) ? DBNull.Value : ch.CardholderPhotoURL));
                cmd.Parameters.AddWithValue("key", (object) (string.IsNullOrEmpty(ch.CardholderPhotoKey) ? DBNull.Value : ch.CardholderPhotoKey));

                try
                {
                    await cmd.ExecuteScalarAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error inserting cardholder");
                    throw;
                }
            }
        }
    }
}
