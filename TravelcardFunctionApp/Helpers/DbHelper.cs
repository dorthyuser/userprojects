using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using TravelcardFunctionApp.Models;

namespace TravelcardFunctionApp.Helpers
{
    public class DbHelper : IDbHelper
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger _logger;

        public DbHelper(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DbHelper>();
            var connectionString = configuration["PostgresConnectionString"] ?? "Host=localhost;Username=postgres;Password=postgres;Database=travelcards";
            var builder = new NpgsqlDataSourceBuilder(connectionString);
            // Map enums
            builder.MapEnum<TravelcardFunctionApp.Models.TravelcardType>("travelcard_type_enum");
            builder.MapEnum<TravelcardFunctionApp.Models.CardholderType>("cardholder_type_enum");
            _dataSource = builder.Build();
        }

        public async Task<int> InsertTravelcardAsync(TravelcardRequest request, string token)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                const string travelcardSql = @"INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to) VALUES (@travelcardType::travelcard_type_enum, @validFrom, @validTo, @name, @number, @requested, @transactionRef, @usableTo) RETURNING id;";
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = travelcardSql;
                cmd.Parameters.AddWithValue("@travelcardType", request.TravelcardType.ToString());
                cmd.Parameters.AddWithValue("@validFrom", request.TravelcardValidFrom);
                cmd.Parameters.AddWithValue("@validTo", request.TravelcardValidTo);
                cmd.Parameters.AddWithValue("@name", string.IsNullOrWhiteSpace(request.TravelcardName) ? (object)DBNull.Value : request.TravelcardName);
                cmd.Parameters.AddWithValue("@number", request.TravelcardNumber);
                cmd.Parameters.AddWithValue("@requested", request.TravelcardRequestedDate);
                cmd.Parameters.AddWithValue("@transactionRef", request.TravelcardTransactionReference);
                cmd.Parameters.AddWithValue("@usableTo", request.TravelcardUsableTo.HasValue ? (object)request.TravelcardUsableTo.Value : DBNull.Value);
                cmd.Transaction = tx;

                var travelcardIdObj = await cmd.ExecuteScalarAsync();
                if (travelcardIdObj == null)
                {
                    throw new Exception("Failed to insert travelcard");
                }

                var travelcardId = Convert.ToInt32(travelcardIdObj);

                const string cardholderSql = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key) VALUES (@travelcardId, @title, @forename, @surname, @type::cardholder_type_enum, @photoName, @photoRrsKey, @photoUrl, @photoKey);";

                foreach (var ch in request.Cardholders)
                {
                    await using var cmd2 = conn.CreateCommand();
                    cmd2.CommandText = cardholderSql;
                    cmd2.Parameters.AddWithValue("@travelcardId", travelcardId);
                    cmd2.Parameters.AddWithValue("@title", ch.CardholderTitle);
                    cmd2.Parameters.AddWithValue("@forename", ch.CardholderForename);
                    cmd2.Parameters.AddWithValue("@surname", ch.CardholderSurname);
                    cmd2.Parameters.AddWithValue("@type", ch.CardholderType.ToString());
                    cmd2.Parameters.AddWithValue("@photoName", ch.CardholderPhotoName);
                    cmd2.Parameters.AddWithValue("@photoRrsKey", string.IsNullOrWhiteSpace(ch.CardholderPhotoRRSKey) ? (object)DBNull.Value : ch.CardholderPhotoRRSKey);
                    cmd2.Parameters.AddWithValue("@photoUrl", string.IsNullOrWhiteSpace(ch.CardholderPhotoURL) ? (object)DBNull.Value : ch.CardholderPhotoURL);
                    cmd2.Parameters.AddWithValue("@photoKey", string.IsNullOrWhiteSpace(ch.CardholderPhotoKey) ? (object)DBNull.Value : ch.CardholderPhotoKey);
                    cmd2.Transaction = tx;
                    await cmd2.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return travelcardId;
            }
            catch
            {
                try
                {
                    await tx.RollbackAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Rollback failed");
                }

                throw;
            }
        }
    }
}
