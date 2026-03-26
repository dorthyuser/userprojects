using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using TravelcardFunctionApp.Models;

namespace TravelcardFunctionApp.Helpers
{
    public class DbHelper
    {
        private readonly string _connectionString;
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger<DbHelper> _logger;

        public DbHelper(string connectionString, ILogger<DbHelper> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var builder = new NpgsqlDataSourceBuilder(_connectionString);
            builder.MapEnum<TravelcardType>("travelcard_type_enum");
            builder.MapEnum<CardholderType>("cardholder_type_enum");
            _dataSource = builder.Build();
        }

        public async Task<int> InsertTravelcardAsync(TravelcardRequest request, Guid travelcardGuid, string token, ILogger logger)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var tran = await conn.BeginTransactionAsync();
            try
            {
                // insert travelcard
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
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
 @travelcardType::travelcard_type_enum,
 @travelcardValidFrom,
 @travelcardValidTo,
 @travelcardName,
 @travelcardNumber,
 @travelcardRequestedDate,
 @travelcardTransactionReference,
 @travelcardUsableTo
) RETURNING id;";

                    cmd.Parameters.AddWithValue("@travelcardType", request.TravelcardType!.ToString());
                    cmd.Parameters.AddWithValue("@travelcardValidFrom", request.TravelcardValidFrom!.Value);
                    cmd.Parameters.AddWithValue("@travelcardValidTo", request.TravelcardValidTo!.Value);
                    cmd.Parameters.AddWithValue("@travelcardName", (object?)request.TravelcardName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@travelcardNumber", (object?)request.TravelcardNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@travelcardRequestedDate", request.TravelcardRequestedDate!.Value);
                    cmd.Parameters.AddWithValue("@travelcardTransactionReference", request.TravelcardTransactionReference!);
                    cmd.Parameters.AddWithValue("@travelcardUsableTo", (object?)request.TravelcardUsableTo ?? DBNull.Value);

                    var result = await cmd.ExecuteScalarAsync();
                    var travelcardId = Convert.ToInt32(result);

                    // insert cardholders
                    foreach (var ch in request.Cardholders)
                    {
                        await using var chCmd = conn.CreateCommand();
                        chCmd.CommandText = @"
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
 @cardholderTitle,
 @cardholderForename,
 @cardholderSurname,
 @cardholderType::cardholder_type_enum,
 @cardholderPhotoName,
 @cardholderPhotoRrsKey,
 @cardholderPhotoUrl,
 @cardholderPhotoKey
);";

                        chCmd.Parameters.AddWithValue("@travelcardId", travelcardId);
                        chCmd.Parameters.AddWithValue("@cardholderTitle", ch.CardholderTitle);
                        chCmd.Parameters.AddWithValue("@cardholderForename", ch.CardholderForename);
                        chCmd.Parameters.AddWithValue("@cardholderSurname", ch.CardholderSurname);
                        chCmd.Parameters.AddWithValue("@cardholderType", ch.CardholderType.ToString());
                        chCmd.Parameters.AddWithValue("@cardholderPhotoName", ch.CardholderPhotoName);
                        chCmd.Parameters.AddWithValue("@cardholderPhotoRrsKey", (object?)ch.CardholderPhotoRrsKey ?? DBNull.Value);
                        chCmd.Parameters.AddWithValue("@cardholderPhotoUrl", (object?)ch.CardholderPhotoUrl ?? DBNull.Value);
                        chCmd.Parameters.AddWithValue("@cardholderPhotoKey", (object?)ch.CardholderPhotoKey ?? DBNull.Value);

                        await chCmd.ExecuteNonQueryAsync();
                    }

                    await tran.CommitAsync();
                    return travelcardId;
                }
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                _logger.LogError(ex, "Error inserting travelcard and cardholders");
                throw;
            }
        }
    }
}
