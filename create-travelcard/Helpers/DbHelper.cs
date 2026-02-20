using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using CreateTravelcard.Models;
using System.Text.Json;

namespace CreateTravelcard.Helpers
{
    public class DbHelper : IDisposable
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger<DbHelper> _logger;
        private readonly TokenGenerator _tokenGenerator;

        public DbHelper(IConfiguration configuration, ILogger<DbHelper> logger, TokenGenerator tokenGenerator)
        {
            _logger = logger;
            _tokenGenerator = tokenGenerator;
            var connString = configuration.GetValue<string>("PostgresConnectionString");
            if (string.IsNullOrWhiteSpace(connString)) throw new ArgumentException("PostgresConnectionString is not configured");
            var builder = new NpgsqlDataSourceBuilder(connString);
            // Enable simple pooling defaults
            _dataSource = builder.Build();
        }

        public async Task<(Guid travelcardId, string token)> InsertTravelcardAsync(TravelcardRequest payload, string clientId, string correlationId)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                var travelcardId = Guid.NewGuid();
                var token = _tokenGenerator.GenerateToken();

                // Insert travelcard
                var insertTravelcardCmd = conn.CreateCommand();
                insertTravelcardCmd.Transaction = tx;
                insertTravelcardCmd.CommandText = @"
INSERT INTO travelcards (id, client_id, correlation_id, travelcard_type, valid_from, valid_to, name, number, requested_date, transaction_reference, usable_to, created_at, token)
VALUES (@id, @clientId, @correlationId, @type, @validFrom, @validTo, @name, @number, @requestedDate, @transactionReference, @usableTo, @createdAt, @token)
";
                insertTravelcardCmd.Parameters.AddWithValue("@id", NpgsqlDbType.Uuid, travelcardId);
                insertTravelcardCmd.Parameters.AddWithValue("@clientId", NpgsqlDbType.Text, clientId);
                insertTravelcardCmd.Parameters.AddWithValue("@correlationId", NpgsqlDbType.Text, (object)correlationId ?? DBNull.Value);
                insertTravelcardCmd.Parameters.AddWithValue("@type", NpgsqlDbType.Text, payload.TravelcardType.ToString());
                insertTravelcardCmd.Parameters.AddWithValue("@validFrom", NpgsqlDbType.TimestampTz, payload.TravelcardValidFrom);
                insertTravelcardCmd.Parameters.AddWithValue("@validTo", NpgsqlDbType.TimestampTz, payload.TravelcardValidTo);
                insertTravelcardCmd.Parameters.AddWithValue("@name", NpgsqlDbType.Text, (object)payload.TravelcardName ?? DBNull.Value);
                insertTravelcardCmd.Parameters.AddWithValue("@number", NpgsqlDbType.Text, payload.TravelcardNumber);
                insertTravelcardCmd.Parameters.AddWithValue("@requestedDate", NpgsqlDbType.TimestampTz, payload.TravelcardRequestedDate);
                insertTravelcardCmd.Parameters.AddWithValue("@transactionReference", NpgsqlDbType.Text, payload.TravelcardTransactionReference);
                insertTravelcardCmd.Parameters.AddWithValue("@usableTo", NpgsqlDbType.TimestampTz, payload.TravelcardUsableTo.HasValue ? (object)payload.TravelcardUsableTo.Value : DBNull.Value);
                insertTravelcardCmd.Parameters.AddWithValue("@createdAt", NpgsqlDbType.TimestampTz, DateTime.UtcNow);
                insertTravelcardCmd.Parameters.AddWithValue("@token", NpgsqlDbType.Text, token);

                await insertTravelcardCmd.ExecuteNonQueryAsync();

                // Insert cardholders
                foreach (var ch in payload.Cardholders)
                {
                    var cardholderId = Guid.NewGuid();
                    var insertCardholder = conn.CreateCommand();
                    insertCardholder.Transaction = tx;
                    insertCardholder.CommandText = @"
INSERT INTO cardholders (id, travelcard_id, cardholder_type, title, forename, surname, photo_name, photo_rrs_key, photo_key, photo_url)
VALUES (@id, @travelcardId, @type, @title, @forename, @surname, @photoName, @photoRrsKey, @photoKey, @photoUrl)
";
                    insertCardholder.Parameters.AddWithValue("@id", NpgsqlDbType.Uuid, cardholderId);
                    insertCardholder.Parameters.AddWithValue("@travelcardId", NpgsqlDbType.Uuid, travelcardId);
                    insertCardholder.Parameters.AddWithValue("@type", NpgsqlDbType.Text, ch.CardholderType.ToString());
                    insertCardholder.Parameters.AddWithValue("@title", NpgsqlDbType.Text, ch.CardholderTitle);
                    insertCardholder.Parameters.AddWithValue("@forename", NpgsqlDbType.Text, ch.CardholderForename);
                    insertCardholder.Parameters.AddWithValue("@surname", NpgsqlDbType.Text, ch.CardholderSurname);
                    insertCardholder.Parameters.AddWithValue("@photoName", NpgsqlDbType.Text, ch.CardholderPhotoName);
                    insertCardholder.Parameters.AddWithValue("@photoRrsKey", NpgsqlDbType.Text, (object)ch.CardholderPhotoRRSKey ?? DBNull.Value);
                    insertCardholder.Parameters.AddWithValue("@photoKey", NpgsqlDbType.Text, (object)ch.CardholderPhotoKey ?? DBNull.Value);
                    insertCardholder.Parameters.AddWithValue("@photoUrl", NpgsqlDbType.Text, (object)ch.CardholderPhotoURL ?? DBNull.Value);

                    await insertCardholder.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return (travelcardId, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database insert failed");
                try
                {
                    await tx.RollbackAsync();
                }
                catch { }
                throw;
            }
        }

        public void Dispose()
        {
            _dataSource?.Dispose();
        }
    }
}
