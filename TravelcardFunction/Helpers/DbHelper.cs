using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using TravelcardFunction.Models;
using System.Text.Json;
using NpgsqlTypes;

namespace TravelcardFunction.Helpers
{
    public interface IDbHelper
    {
        Task<(string TravelcardGuid, string Token)> CreateTravelcardAsync(CreateTravelcardRequest request, string clientId, string correlationId);
    }

    public class DbHelper : IDbHelper, IDisposable
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger _logger;

        public DbHelper(NpgsqlDataSource dataSource, ILoggerFactory loggerFactory)
        {
            _dataSource = dataSource;
            _logger = loggerFactory.CreateLogger<DbHelper>();
        }

        public async Task<(string TravelcardGuid, string Token)> CreateTravelcardAsync(CreateTravelcardRequest request, string clientId, string correlationId)
        {
            _logger.LogInformation("DB: Enter CreateTravelcardAsync");
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                // Insert travelcard
                var travelcardIdSql = @"INSERT INTO public.travelcards (
                    travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to
                    ) VALUES (@type, @validFrom, @validTo, @name, @number, @requestedDate, @transactionRef, @usableTo) RETURNING id";

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = travelcardIdSql;
                cmd.Parameters.Add(new NpgsqlParameter("@type", NpgsqlDbType.Varchar) { Value = request.TravelcardType.ToString() });
                cmd.Parameters.Add(new NpgsqlParameter("@validFrom", NpgsqlDbType.TimestampTz) { Value = request.TravelcardValidFrom });
                cmd.Parameters.Add(new NpgsqlParameter("@validTo", NpgsqlDbType.TimestampTz) { Value = request.TravelcardValidTo });
                cmd.Parameters.Add(new NpgsqlParameter("@name", NpgsqlDbType.Varchar) { Value = (object)request.TravelcardName ?? DBNull.Value });
                cmd.Parameters.Add(new NpgsqlParameter("@number", NpgsqlDbType.Varchar) { Value = request.TravelcardNumber });
                cmd.Parameters.Add(new NpgsqlParameter("@requestedDate", NpgsqlDbType.TimestampTz) { Value = request.TravelcardRequestedDate });
                cmd.Parameters.AddWithValue("@transactionRef", request.TravelcardTransactionReference );
                cmd.Parameters.Add(new NpgsqlParameter("@usableTo", NpgsqlDbType.TimestampTz) { Value = (object)request.TravelcardUsableTo ?? DBNull.Value });

                var newIdObj = await cmd.ExecuteScalarAsync();
                var dbId = Convert.ToInt32(newIdObj);

                // Insert cardholders
                var insertCardholderSql = @"INSERT INTO public.cardholders (
                    travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key
                    ) VALUES (@travelcardId, @title, @forename, @surname, @type, @photoName, @photoRrsKey, @photoUrl, @photoKey)";

                foreach (var ch in request.Cardholders)
                {
                    await using var cmd2 = conn.CreateCommand();
                    cmd2.CommandText = insertCardholderSql;
                    cmd2.Parameters.Add(new NpgsqlParameter("@travelcardId", NpgsqlDbType.Integer) { Value = dbId });
                    cmd2.Parameters.Add(new NpgsqlParameter("@title", NpgsqlDbType.Varchar) { Value = ch.CardholderTitle });
                    cmd2.Parameters.Add(new NpgsqlParameter("@forename", NpgsqlDbType.Varchar) { Value = ch.CardholderForename });
                    cmd2.Parameters.Add(new NpgsqlParameter("@surname", NpgsqlDbType.Varchar) { Value = ch.CardholderSurname });
                    cmd2.Parameters.Add(new NpgsqlParameter("@type", NpgsqlDbType.Varchar) { Value = ch.CardholderType.ToString() });
                    cmd2.Parameters.Add(new NpgsqlParameter("@photoName", NpgsqlDbType.Varchar) { Value = ch.CardholderPhotoName });
                    cmd2.Parameters.Add(new NpgsqlParameter("@photoRrsKey", NpgsqlDbType.Varchar) { Value = (object)ch.CardholderPhotoRrsKey ?? DBNull.Value });
                    cmd2.Parameters.Add(new NpgsqlParameter("@photoUrl", NpgsqlDbType.Varchar) { Value = (object)ch.CardholderPhotoUrl ?? DBNull.Value });
                    cmd2.Parameters.Add(new NpgsqlParameter("@photoKey", NpgsqlDbType.Varchar) { Value = (object)ch.CardholderPhotoKey ?? DBNull.Value });
                    await cmd2.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                // Generate GUID and token to return; GUID is external identifier (not stored in schema)
                var travelcardGuid = Guid.NewGuid().ToString();
                var token = TokenGenerator.GenerateToken(6);

                _logger.LogInformation("DB: Created travelcard id {dbId}", dbId);
                return (travelcardGuid, token);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "DB: Error creating travelcard");
                throw;
            }
        }

        public void Dispose()
        {
            // NpgsqlDataSource does not need dispose here; kept for interface
        }
    }

    public static class TokenGenerator
    {
        private static readonly char[] _chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
        public static string GenerateToken(int length)
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var data = new byte[length];
            rng.GetBytes(data);
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = _chars[data[i] % _chars.Length];
            }
            return new string(result);
        }
    }
}
