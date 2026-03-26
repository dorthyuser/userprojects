using System;
using System.Threading.Tasks;
using Npgsql;
using System.Collections.Generic;
using TravelcardFunction.Models;
using Microsoft.Extensions.Logging;

namespace TravelcardFunction.Helpers
{
    public class DbHelper
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger _logger;

        public DbHelper(string connectionString, ILogger<DbHelper> logger)
        {
            _logger = logger;
            var builder = new NpgsqlDataSourceBuilder(connectionString);
            builder.MapEnum<TravelcardType>("travelcard_type_enum");
            builder.MapEnum<CardholderType>("cardholder_type_enum");
            _dataSource = builder.Build();
        }

        public async Task<int> InsertTravelcardAsync(TravelcardEntity travelcard, List<CardholderCreateRequest> cardholders)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                // Insert travelcard
                var insertSql = @"INSERT INTO public.travelcards
(travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES
(@travelcardType::travelcard_type_enum, @travelcardValidFrom, @travelcardValidTo, @travelcardName, @travelcardNumber, @travelcardRequestedDate, @travelcardTransactionReference, @travelcardUsableTo)
RETURNING id";

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = insertSql;
                cmd.Parameters.AddWithValue("travelcardType", travelcard.TravelcardType.ToString());
                cmd.Parameters.AddWithValue("travelcardValidFrom", travelcard.TravelcardValidFrom);
                cmd.Parameters.AddWithValue("travelcardValidTo", travelcard.TravelcardValidTo);
                cmd.Parameters.AddWithValue("travelcardName", (object?) (string.IsNullOrWhiteSpace(travelcard.TravelcardName) ? DBNull.Value : travelcard.TravelcardName));
                cmd.Parameters.AddWithValue("travelcardNumber", (object?) travelcard.TravelcardNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("travelcardRequestedDate", travelcard.TravelcardRequestedDate);
                cmd.Parameters.AddWithValue("travelcardTransactionReference", travelcard.TravelcardTransactionReference);
                cmd.Parameters.AddWithValue("travelcardUsableTo", (object?) travelcard.TravelcardUsableTo ?? DBNull.Value);

                var result = await cmd.ExecuteScalarAsync();
                var travelcardId = Convert.ToInt32(result);

                // Insert cardholders
                var insertCardholderSql = @"INSERT INTO public.cardholders
(travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES
(@travelcardId, @title, @forename, @surname, @type::cardholder_type_enum, @photoName, @photoRrsKey, @photoUrl, @photoKey)";

                foreach (var ch in cardholders)
                {
                    await using var cmdCh = conn.CreateCommand();
                    cmdCh.CommandText = insertCardholderSql;
                    cmdCh.Parameters.AddWithValue("travelcardId", travelcardId);
                    cmdCh.Parameters.AddWithValue("title", ch.CardholderTitle);
                    cmdCh.Parameters.AddWithValue("forename", ch.CardholderForename);
                    cmdCh.Parameters.AddWithValue("surname", ch.CardholderSurname);
                    cmdCh.Parameters.AddWithValue("type", ch.CardholderType.ToString());
                    cmdCh.Parameters.AddWithValue("photoName", ch.CardholderPhotoName);
                    cmdCh.Parameters.AddWithValue("photoRrsKey", (object?) ch.CardholderPhotoRrsKey ?? DBNull.Value);
                    cmdCh.Parameters.AddWithValue("photoUrl", (object?) ch.CardholderPhotoUrl ?? DBNull.Value);
                    cmdCh.Parameters.AddWithValue("photoKey", (object?) ch.CardholderPhotoKey ?? DBNull.Value);
                    await cmdCh.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                _logger.LogInformation("Inserted travelcard with db id {id}", travelcardId);
                return travelcardId;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error inserting travelcard and cardholders");
                throw;
            }
        }
    }
}
