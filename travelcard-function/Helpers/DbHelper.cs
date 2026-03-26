using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using travelcard_function.Models;

namespace travelcard_function.Helpers
{
    public interface IDbHelper
    {
        Task<DbResult> CreateTravelcardAsync(CreateTravelcardRequest request);
    }

    public class DbResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string TravelcardGuid { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    public class DbHelper : IDbHelper, IDisposable
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger _logger;

        public DbHelper(string connectionString, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DbHelper>();
            var builder = new NpgsqlDataSourceBuilder(connectionString);
            // Map enums to Postgres enum names
            builder.MapEnum<travelcard_function.Models.TravelcardType>("travelcard_type_enum");
            builder.MapEnum<travelcard_function.Models.CardholderType>("cardholder_type_enum");
            _dataSource = builder.Build();
        }

        public async Task<DbResult> CreateTravelcardAsync(CreateTravelcardRequest request)
        {
            var result = new DbResult();
            // Generate GUID and token for response (business requirement)
            var travelcardGuid = Guid.NewGuid().ToString();
            var token = GenerateToken();
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                await using var tx = await conn.BeginTransactionAsync();

                var insertTravelcard = @"
INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES (@type::travelcard_type_enum, @validFrom, @validTo, @name, @number, @requestedDate, @transactionRef, @usableTo)
RETURNING id";

                await using (var cmd = new NpgsqlCommand(insertTravelcard, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@type", request.travelcardType.ToString());
                    cmd.Parameters.AddWithValue("@validFrom", request.travelcardValidFrom);
                    cmd.Parameters.AddWithValue("@validTo", request.travelcardValidTo);
                    if (!string.IsNullOrWhiteSpace(request.travelcardName)) cmd.Parameters.AddWithValue("@name", request.travelcardName); else cmd.Parameters.AddWithValue("@name", DBNull.Value);
                    cmd.Parameters.AddWithValue("@number", request.travelcardNumber);
                    cmd.Parameters.AddWithValue("@requestedDate", request.travelcardRequestedDate);
                    cmd.Parameters.AddWithValue("@transactionRef", request.travelcardTransactionReference);
                    if (request.travelcardUsableTo.HasValue) cmd.Parameters.AddWithValue("@usableTo", request.travelcardUsableTo.Value); else cmd.Parameters.AddWithValue("@usableTo", DBNull.Value);

                    var insertedId = (int)await cmd.ExecuteScalarAsync();

                    foreach (var ch in request.cardholders)
                    {
                        var insertCh = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcardId, @title, @forename, @surname, @type::cardholder_type_enum, @photoName, @photoRrsKey, @photoUrl, @photoKey)";

                        await using var cmd2 = new NpgsqlCommand(insertCh, conn, tx);
                        cmd2.Parameters.AddWithValue("@travelcardId", insertedId);
                        cmd2.Parameters.AddWithValue("@title", ch.cardholderTitle);
                        cmd2.Parameters.AddWithValue("@forename", ch.cardholderForename);
                        cmd2.Parameters.AddWithValue("@surname", ch.cardholderSurname);
                        cmd2.Parameters.AddWithValue("@type", ch.cardholderType.ToString());
                        cmd2.Parameters.AddWithValue("@photoName", ch.cardholderPhotoName);
                        if (!string.IsNullOrWhiteSpace(ch.cardholderPhotoRRSKey)) cmd2.Parameters.AddWithValue("@photoRrsKey", ch.cardholderPhotoRRSKey); else cmd2.Parameters.AddWithValue("@photoRrsKey", DBNull.Value);
                        if (!string.IsNullOrWhiteSpace(ch.cardholderPhotoURL)) cmd2.Parameters.AddWithValue("@photoUrl", ch.cardholderPhotoURL); else cmd2.Parameters.AddWithValue("@photoUrl", DBNull.Value);
                        if (!string.IsNullOrWhiteSpace(ch.cardholderPhotoKey)) cmd2.Parameters.AddWithValue("@photoKey", ch.cardholderPhotoKey); else cmd2.Parameters.AddWithValue("@photoKey", DBNull.Value);
                        await cmd2.ExecuteNonQueryAsync();
                    }

                    await tx.CommitAsync();
                    result.Success = true;
                    result.TravelcardGuid = travelcardGuid;
                    result.Token = token;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating travelcard");
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private static string GenerateToken()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var rng = new Random();
            var buffer = new char[6];
            for (var i = 0; i < 6; i++) buffer[i] = chars[rng.Next(chars.Length)];
            return new string(buffer);
        }

        public void Dispose()
        {
            _dataSource?.Dispose();
        }
    }
}
