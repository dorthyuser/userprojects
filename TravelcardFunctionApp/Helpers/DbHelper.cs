using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Npgsql;
using NpgsqlTypes;
using Npgsql.Replication;
using Npgsql.PostgresTypes;
using Npgsql.Internal.TypeLoading;
using Npgsql.Logging;
using System.Data;
using TravelcardFunctionApp.Models;

namespace TravelcardFunctionApp.Helpers
{
    public class DbException : Exception
    {
        public DbException(string message, Exception inner = null) : base(message, inner) { }
    }

    public class DbHelper
    {
        private readonly NpgsqlDataSource _dataSource;

        public DbHelper()
        {
            var connectionString = Environment.GetEnvironmentVariable("PostgresConnectionString");
            if (string.IsNullOrEmpty(connectionString))
            {
                // Dummy value to avoid empty string but still invalid in production
                connectionString = "Host=localhost;Port=5432;Database=travelcards;Username=postgres;Password=postgres";
            }

            var builder = new NpgsqlDataSourceBuilder(connectionString);
            // Register enums
            builder.MapEnum<TravelcardType>("travelcard_type_enum");
            builder.MapEnum<CardholderType>("cardholder_type_enum");

            _dataSource = builder.Build();
        }

        public async Task<int> InsertTravelcardAsync(TravelcardRequest travelcard)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                const string insertTravelcardSql = @"
INSERT INTO public.travelcards
(travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES (@type::travelcard_type_enum, @valid_from, @valid_to, @name, @number, @requested_date, @transaction_ref, @usable_to)
RETURNING id;";

                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = insertTravelcardSql;
                    cmd.Parameters.AddWithValue("type", travelcard.TravelcardType.ToString());
                    cmd.Parameters.AddWithValue("valid_from", travelcard.TravelcardValidFrom);
                    cmd.Parameters.AddWithValue("valid_to", travelcard.TravelcardValidTo);
                    cmd.Parameters.AddWithValue("name", string.IsNullOrWhiteSpace(travelcard.TravelcardName) ? (object)DBNull.Value : travelcard.TravelcardName);
                    cmd.Parameters.AddWithValue("number", travelcard.TravelcardNumber);
                    cmd.Parameters.AddWithValue("requested_date", travelcard.TravelcardRequestedDate);
                    cmd.Parameters.AddWithValue("transaction_ref", travelcard.TravelcardTransactionReference);
                    cmd.Parameters.AddWithValue("usable_to", travelcard.TravelcardUsableTo.HasValue ? (object)travelcard.TravelcardUsableTo.Value : DBNull.Value);

                    var result = await cmd.ExecuteScalarAsync();
                    int travelcardId = Convert.ToInt32(result);

                    // Insert cardholders
                    const string insertCardholderSql = @"
INSERT INTO public.cardholders
(travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcard_id, @title, @forename, @surname, @type::cardholder_type_enum, @photo_name, @photo_rrs_key, @photo_url, @photo_key);";

                    foreach (var ch in travelcard.Cardholders)
                    {
                        await using var cmdCh = conn.CreateCommand();
                        cmdCh.CommandText = insertCardholderSql;
                        cmdCh.Parameters.AddWithValue("travelcard_id", travelcardId);
                        cmdCh.Parameters.AddWithValue("title", ch.CardholderTitle);
                        cmdCh.Parameters.AddWithValue("forename", ch.CardholderForename);
                        cmdCh.Parameters.AddWithValue("surname", ch.CardholderSurname);
                        cmdCh.Parameters.AddWithValue("type", ch.CardholderType.ToString());
                        cmdCh.Parameters.AddWithValue("photo_name", ch.CardholderPhotoName);
                        cmdCh.Parameters.AddWithValue("photo_rrs_key", string.IsNullOrWhiteSpace(ch.CardholderPhotoRRSKey) ? (object)DBNull.Value : ch.CardholderPhotoRRSKey);
                        cmdCh.Parameters.AddWithValue("photo_url", string.IsNullOrWhiteSpace(ch.CardholderPhotoURL) ? (object)DBNull.Value : ch.CardholderPhotoURL);
                        cmdCh.Parameters.AddWithValue("photo_key", string.IsNullOrWhiteSpace(ch.CardholderPhotoKey) ? (object)DBNull.Value : ch.CardholderPhotoKey);

                        await cmdCh.ExecuteNonQueryAsync();
                    }

                    await tx.CommitAsync();
                    return travelcardId;
                }
            }
            catch (Exception ex)
            {
                try { await tx.RollbackAsync(); } catch { }
                throw new DbException("Failed to insert travelcard and cardholders.", ex);
            }
        }
    }
}
