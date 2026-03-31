using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using travelcard_http.Models;

namespace travelcard_http.Helpers
{
    /// <summary>
    /// DB helper responsible for persisting travelcards and cardholders.
    /// Registers Postgres enums and uses NpgsqlDataSourceBuilder as required.
    /// Expects PG_CONNECTION_STRING environment variable to be set.
    /// </summary>
    public class DbHelper
    {
        private readonly string _connectionString;
        private readonly ILogger<DbHelper> _logger;
        private readonly NpgsqlDataSource _dataSource;

        public DbHelper(IConfiguration configuration, ILogger<DbHelper> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionString = configuration["PG_CONNECTION_STRING"] ?? Environment.GetEnvironmentVariable("PG_CONNECTION_STRING") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                _logger.LogError("PG_CONNECTION_STRING is not configured");
                throw new InvalidOperationException("PG_CONNECTION_STRING must be configured");
            }

            // Map enums per rules using NpgsqlDataSourceBuilder
            var builder = new NpgsqlDataSourceBuilder(_connectionString);
            builder.MapEnum<TravelcardType>("travelcard_type_enum");
            builder.MapEnum<CardholderType>("cardholder_type_enum");

            _dataSource = builder.Build();
        }

        /// <summary>
        /// Inserts a travelcard and its cardholders and returns the created id as int.
        /// </summary>
        public async Task<int> InsertTravelcardAsync(TravelcardRequest model)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                var insertTravelcardSql = @"
INSERT INTO public.travelcards (
    travelcard_type,
    travelcard_valid_from,
    travelcard_valid_to,
    travelcard_name,
    travelcard_number,
    travelcard_requested_date,
    travelcard_transaction_reference,
    travelcard_usable_to
) VALUES (
    @type::travelcard_type_enum,
    @validFrom,
    @validTo,
    @name,
    @number,
    @requestedDate,
    @txRef,
    @usableTo
) RETURNING id;";

                await using var cmd = new NpgsqlCommand(insertTravelcardSql, conn);
                cmd.Transaction = tx;
                cmd.Parameters.AddWithValue("type", model.TravelcardType.ToString());
                cmd.Parameters.AddWithValue("validFrom", model.TravelcardValidFrom);
                cmd.Parameters.AddWithValue("validTo", model.TravelcardValidTo);
                cmd.Parameters.AddWithValue("name", (object?)model.TravelcardName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("number", (object?)model.TravelcardNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("requestedDate", model.TravelcardRequestedDate);
                cmd.Parameters.AddWithValue("txRef", model.TravelcardTransactionReference);
                cmd.Parameters.AddWithValue("usableTo", (object?)model.TravelcardUsableTo ?? DBNull.Value);

                var idObj = await cmd.ExecuteScalarAsync();
                var travelcardId = Convert.ToInt32(idObj);

                var insertHolderSql = @"
INSERT INTO public.cardholders (
    travelcard_id,
    cardholder_title,
    cardholder_forename,
    cardholder_surname,
    cardholder_type,
    cardholder_photo_name,
    cardholder_photo_rrs_key,
    cardholder_photo_url,
    cardholder_photo_key
) VALUES (
    @travelcardId,
    @title,
    @forename,
    @surname,
    @type::cardholder_type_enum,
    @photoName,
    @photoRrsKey,
    @photoUrl,
    @photoKey
);";

                foreach (var ch in model.Cardholders)
                {
                    await using var cmdH = new NpgsqlCommand(insertHolderSql, conn);
                    cmdH.Transaction = tx;
                    cmdH.Parameters.AddWithValue("travelcardId", travelcardId);
                    cmdH.Parameters.AddWithValue("title", ch.CardholderTitle);
                    cmdH.Parameters.AddWithValue("forename", ch.CardholderForename);
                    cmdH.Parameters.AddWithValue("surname", ch.CardholderSurname);
                    cmdH.Parameters.AddWithValue("type", ch.CardholderType.ToString());
                    cmdH.Parameters.AddWithValue("photoName", ch.CardholderPhotoName);
                    cmdH.Parameters.AddWithValue("photoRrsKey", (object?)ch.CardholderPhotoRrsKey ?? DBNull.Value);
                    cmdH.Parameters.AddWithValue("photoUrl", (object?)ch.CardholderPhotoUrl ?? DBNull.Value);
                    cmdH.Parameters.AddWithValue("photoKey", (object?)ch.CardholderPhotoKey ?? DBNull.Value);

                    await cmdH.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                return travelcardId;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error inserting travelcard");
                throw;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }
}
