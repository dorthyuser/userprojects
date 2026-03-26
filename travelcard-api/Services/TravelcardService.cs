using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using TravelcardApi.Helpers;
using TravelcardApi.Models;

namespace TravelcardApi.Services
{
    public class TravelcardService
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger _logger;

        public TravelcardService(DatabaseHelper db, ILogger<TravelcardService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<TravelcardResponse> CreateTravelcardAsync(TravelcardRequest req, string clientId, string? correlationId)
        {
            _logger.LogInformation("Enter TravelcardService.CreateTravelcard client={ClientId} corr={Corr}", clientId, correlationId ?? "-");

            // Validate business rules
            Validators.Validate(req);

            // Persist to Postgres
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            try
            {
                // Insert travelcard
                var insertTravelcard = @"INSERT INTO travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES (@travelcardType::travelcard_type_enum, @validFrom, @validTo, @name, @number, @requestedDate, @transactionRef, @usableTo)
RETURNING id;";

                await using var cmd = new NpgsqlCommand(insertTravelcard, conn, tx);
                // Do not use NpgsqlDbType for enums - send as string and cast in SQL
                cmd.Parameters.AddWithValue("travelcardType", req.travelcardType.ToString());
                cmd.Parameters.AddWithValue("validFrom", req.travelcardValidFrom.ToUniversalTime());
                cmd.Parameters.AddWithValue("validTo", req.travelcardValidTo.ToUniversalTime());
                cmd.Parameters.AddWithValue("name", string.IsNullOrEmpty(req.travelcardName) ? (object)DBNull.Value : req.travelcardName);
                cmd.Parameters.AddWithValue("number", req.travelcardNumber);
                cmd.Parameters.AddWithValue("requestedDate", req.travelcardRequestedDate.ToUniversalTime());
                cmd.Parameters.AddWithValue("transactionRef", req.travelcardTransactionReference);
                cmd.Parameters.AddWithValue("usableTo", req.travelcardUsableTo.HasValue ? (object)req.travelcardUsableTo.Value.ToUniversalTime() : DBNull.Value);

                var travelcardIdInt = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                // Insert cardholders
                foreach (var ch in req.cardholders)
                {
                    var insertCardholder = @"INSERT INTO cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcardId, @title, @forename, @surname, @type::cardholder_type_enum, @photoName, @rrsKey, @url, @key);";

                    await using var ccmd = new NpgsqlCommand(insertCardholder, conn, tx);
                    ccmd.Parameters.AddWithValue("travelcardId", travelcardIdInt);
                    ccmd.Parameters.AddWithValue("title", ch.cardholderTitle);
                    ccmd.Parameters.AddWithValue("forename", ch.cardholderForename);
                    ccmd.Parameters.AddWithValue("surname", ch.cardholderSurname);
                    ccmd.Parameters.AddWithValue("type", ch.cardholderType.ToString());
                    ccmd.Parameters.AddWithValue("photoName", ch.cardholderPhotoName);
                    ccmd.Parameters.AddWithValue("rrsKey", string.IsNullOrEmpty(ch.cardholderPhotoRRSKey) ? (object)DBNull.Value : ch.cardholderPhotoRRSKey);
                    ccmd.Parameters.AddWithValue("url", string.IsNullOrEmpty(ch.cardholderPhotoURL) ? (object)DBNull.Value : ch.cardholderPhotoURL);
                    ccmd.Parameters.AddWithValue("key", string.IsNullOrEmpty(ch.cardholderPhotoKey) ? (object)DBNull.Value : ch.cardholderPhotoKey);

                    await ccmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                // Generate response token and GUID
                var response = new TravelcardResponse
                {
                    travelcardId = Guid.NewGuid().ToString(),
                    token = GenerateToken(6)
                };

                _logger.LogInformation("Exit TravelcardService.CreateTravelcard success travelcardId={Guid} dbId={DbId}", response.travelcardId, travelcardIdInt);
                return response;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Database error while creating travelcard");
                throw;
            }
        }

        private static string GenerateToken(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var str = new char[length];
            for (var i = 0; i < length; i++) str[i] = chars[random.Next(chars.Length)];
            return new string(str);
        }
    }
}
