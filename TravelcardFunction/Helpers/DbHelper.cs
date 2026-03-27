using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using TravelcardFunction.Models;

namespace TravelcardFunction.Helpers
{
    public class DbResult
    {
        public int TravelcardDbId { get; set; }
    }

    public class DbHelper
    {
        private readonly string _connectionString;
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger<DbHelper> _logger;

        public DbHelper(string connectionString, ILogger<DbHelper>? logger)
        {
            _connectionString = connectionString;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var builder = new NpgsqlDataSourceBuilder(_connectionString);
            // Map enums as required
            builder.MapEnum<TravelcardType>("travelcard_type_enum");
            builder.MapEnum<CardholderType>("cardholder_type_enum");

            _dataSource = builder.Build();
        }

        public async Task<DbResult> InsertTravelcardAsync(TravelcardRequest req)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                // Insert travelcard
                var insertTravelcardSql = @"
INSERT INTO travelcards
(travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES
(@type::travelcard_type_enum, @validFrom, @validTo, @name, @number, @requestedDate, @transactionReference, @usableTo)
RETURNING id";

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = insertTravelcardSql;
                cmd.Parameters.AddWithValue("type", req.TravelcardType.ToString());
                cmd.Parameters.AddWithValue("validFrom", NpgsqlDbType.TimestampTz, req.TravelcardValidFrom);
                cmd.Parameters.AddWithValue("validTo", NpgsqlDbType.TimestampTz, req.TravelcardValidTo);
                if (string.IsNullOrWhiteSpace(req.TravelcardName))
                    cmd.Parameters.AddWithValue("name", DBNull.Value);
                else
                    cmd.Parameters.AddWithValue("name", req.TravelcardName);
                cmd.Parameters.AddWithValue("number", req.TravelcardNumber);
                cmd.Parameters.AddWithValue("requestedDate", NpgsqlDbType.TimestampTz, req.TravelcardRequestedDate);
                cmd.Parameters.AddWithValue("transactionReference", req.TravelcardTransactionReference);
                if (req.TravelcardUsableTo.HasValue)
                    cmd.Parameters.AddWithValue("usableTo", NpgsqlDbType.TimestampTz, req.TravelcardUsableTo.Value);
                else
                    cmd.Parameters.AddWithValue("usableTo", DBNull.Value);

                var travelcardDbIdObj = await cmd.ExecuteScalarAsync();
                var travelcardDbId = Convert.ToInt32(travelcardDbIdObj);

                // Insert cardholders
                foreach (var ch in req.Cardholders)
                {
                    var insertCardholderSql = @"
INSERT INTO cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcardId, @title, @forename, @surname, @type::cardholder_type_enum, @photoName, @photoRrsKey, @photoUrl, @photoKey)";

                    await using var cmd2 = conn.CreateCommand();
                    cmd2.CommandText = insertCardholderSql;
                    cmd2.Parameters.AddWithValue("travelcardId", travelcardDbId);
                    cmd2.Parameters.AddWithValue("title", ch.CardholderTitle);
                    cmd2.Parameters.AddWithValue("forename", ch.CardholderForename);
                    cmd2.Parameters.AddWithValue("surname", ch.CardholderSurname);
                    cmd2.Parameters.AddWithValue("type", ch.CardholderType.ToString());
                    cmd2.Parameters.AddWithValue("photoName", ch.CardholderPhotoName);
                    if (string.IsNullOrWhiteSpace(ch.CardholderPhotoRrsKey))
                        cmd2.Parameters.AddWithValue("photoRrsKey", DBNull.Value);
                    else
                        cmd2.Parameters.AddWithValue("photoRrsKey", ch.CardholderPhotoRrsKey);
                    if (string.IsNullOrWhiteSpace(ch.CardholderPhotoURL))
                        cmd2.Parameters.AddWithValue("photoUrl", DBNull.Value);
                    else
                        cmd2.Parameters.AddWithValue("photoUrl", ch.CardholderPhotoURL);
                    if (string.IsNullOrWhiteSpace(ch.CardholderPhotoKey))
                        cmd2.Parameters.AddWithValue("photoKey", DBNull.Value);
                    else
                        cmd2.Parameters.AddWithValue("photoKey", ch.CardholderPhotoKey);

                    await cmd2.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return new DbResult { TravelcardDbId = travelcardDbId };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
