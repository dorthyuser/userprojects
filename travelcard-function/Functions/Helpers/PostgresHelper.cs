using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Npgsql;
using TravelcardFunction.Models;

namespace TravelcardFunction.Helpers
{
    public class PostgresHelper
    {
        private readonly NpgsqlDataSource _dataSource;

        public PostgresHelper(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<int> InsertTravelcardAsync(TravelcardCreateRequest request, Guid externalId, FunctionContext context)
        {
            var logger = context.GetLogger("PostgresHelper");
            logger.LogInformation("Enter InsertTravelcardAsync");
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO public.travelcards
(travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES
(@type::travelcard_type_enum, @valid_from, @valid_to, @name, @number, @requested_date, @transaction_ref, @usable_to)
RETURNING id";

                // Ensure values passed as object to avoid nullable conversion warnings
                cmd.Parameters.AddWithValue("type", (object)request.TravelcardType.ToString());
                cmd.Parameters.AddWithValue("valid_from", (object)request.TravelcardValidFrom);
                cmd.Parameters.AddWithValue("valid_to", (object)request.TravelcardValidTo);
                cmd.Parameters.AddWithValue("name", (object?)request.TravelcardName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("number", (object)request.TravelcardNumber);
                cmd.Parameters.AddWithValue("requested_date", (object)request.TravelcardRequestedDate);
                cmd.Parameters.AddWithValue("transaction_ref", (object)request.TravelcardTransactionReference);
                cmd.Parameters.AddWithValue("usable_to", request.TravelcardUsableTo.HasValue ? (object)request.TravelcardUsableTo.Value : DBNull.Value);

                var result = await cmd.ExecuteScalarAsync();
                int id = Convert.ToInt32(result);
                await tx.CommitAsync();
                logger.LogInformation("Inserted travelcard with id {Id}", id);
                logger.LogInformation("Exit InsertTravelcardAsync");
                return id;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                logger.LogError(ex, "Error inserting travelcard");
                throw;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        public async Task InsertCardholdersAsync(int travelcardId, List<CardholderDto> cardholders, FunctionContext context)
        {
            var logger = context.GetLogger("PostgresHelper");
            logger.LogInformation("Enter InsertCardholdersAsync");
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                foreach (var ch in cardholders)
                {
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = @"INSERT INTO public.cardholders
(travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES
(@travelcard_id, @title, @forename, @surname, @type::cardholder_type_enum, @photo_name, @photo_rrs_key, @photo_url, @photo_key)";

                    cmd.Parameters.AddWithValue("travelcard_id", (object)travelcardId);
                    cmd.Parameters.AddWithValue("title", (object)ch.CardholderTitle);
                    cmd.Parameters.AddWithValue("forename", (object)ch.CardholderForename);
                    cmd.Parameters.AddWithValue("surname", (object)ch.CardholderSurname);
                    cmd.Parameters.AddWithValue("type", (object)ch.CardholderType.ToString());
                    cmd.Parameters.AddWithValue("photo_name", (object)ch.CardholderPhotoName);
                    cmd.Parameters.AddWithValue("photo_rrs_key", (object?)ch.CardholderPhotoRrsKey ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("photo_url", (object?)ch.CardholderPhotoUrl ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("photo_key", (object?)ch.CardholderPhotoKey ?? DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                logger.LogInformation("Inserted {Count} cardholders", cardholders.Count);
                logger.LogInformation("Exit InsertCardholdersAsync");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                logger.LogError(ex, "Error inserting cardholders");
                throw;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }
}
