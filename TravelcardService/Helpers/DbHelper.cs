using System;
using System.Threading.Tasks;
using Npgsql;
using TravelcardService.Models;
using Microsoft.Extensions.Configuration;

namespace TravelcardService.Helpers
{
    public class DbInsertResult
    {
        public int TravelcardDbId { get; set; }
    }

    public class DbHelper
    {
        private readonly NpgsqlDataSource _dataSource;

        public DbHelper(IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("PostgresConnectionString");
            if (string.IsNullOrWhiteSpace(connectionString)) connectionString = "Host=localhost;Username=postgres;Password=postgres;Database=travelcards";

            var builder = new NpgsqlDataSourceBuilder(connectionString);
            // Map enums per rules
            builder.MapEnum<TravelcardType>("travelcard_type_enum");
            builder.MapEnum<CardholderType>("cardholder_type_enum");

            _dataSource = builder.Build();
        }

        public async Task<DbInsertResult> InsertTravelcardAsync(TravelcardRequest req, string travelcardGuid)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                // Insert travelcard
                var insertTravelcard = @"INSERT INTO public.travelcards
(travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES (@type::travelcard_type_enum, @validFrom, @validTo, @name, @number, @requestedDate, @transRef, @usableTo)
RETURNING id;";

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = insertTravelcard;
                cmd.Parameters.AddWithValue("type", req.TravelcardType);
                cmd.Parameters.AddWithValue("validFrom", req.TravelcardValidFrom);
                cmd.Parameters.AddWithValue("validTo", req.TravelcardValidTo);
                cmd.Parameters.AddWithValue("name", string.IsNullOrWhiteSpace(req.TravelcardName) ? (object)DBNull.Value : req.TravelcardName);
                cmd.Parameters.AddWithValue("number", req.TravelcardNumber);
                cmd.Parameters.AddWithValue("requestedDate", req.TravelcardRequestedDate);
                cmd.Parameters.AddWithValue("transRef", req.TravelcardTransactionReference);
                cmd.Parameters.AddWithValue("usableTo", req.TravelcardUsableTo == null ? (object)DBNull.Value : req.TravelcardUsableTo);

                var idObj = await cmd.ExecuteScalarAsync();
                var travelcardDbId = Convert.ToInt32(idObj);

                // Insert cardholders
                foreach (var ch in req.Cardholders)
                {
                    var insertCardholder = @"INSERT INTO public.cardholders
(travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcardId, @title, @forename, @surname, @type::cardholder_type_enum, @photoName, @rrsKey, @url, @key);";

                    await using var cmdCh = conn.CreateCommand();
                    cmdCh.CommandText = insertCardholder;
                    cmdCh.Parameters.AddWithValue("travelcardId", travelcardDbId);
                    cmdCh.Parameters.AddWithValue("title", ch.CardholderTitle);
                    cmdCh.Parameters.AddWithValue("forename", ch.CardholderForename);
                    cmdCh.Parameters.AddWithValue("surname", ch.CardholderSurname);
                    cmdCh.Parameters.AddWithValue("type", ch.CardholderType);
                    cmdCh.Parameters.AddWithValue("photoName", ch.CardholderPhotoName);
                    cmdCh.Parameters.AddWithValue("rrsKey", string.IsNullOrWhiteSpace(ch.CardholderPhotoRrsKey) ? (object)DBNull.Value : ch.CardholderPhotoRrsKey);
                    cmdCh.Parameters.AddWithValue("url", string.IsNullOrWhiteSpace(ch.CardholderPhotoUrl) ? (object)DBNull.Value : ch.CardholderPhotoUrl);
                    cmdCh.Parameters.AddWithValue("key", string.IsNullOrWhiteSpace(ch.CardholderPhotoKey) ? (object)DBNull.Value : ch.CardholderPhotoKey);

                    await cmdCh.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return new DbInsertResult { TravelcardDbId = travelcardDbId };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }
}
