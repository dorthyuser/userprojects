using Amazon.Lambda.Core;
using Npgsql;
using Demo_projectLambda.Models;

namespace Demo_projectLambda.Services;

public sealed class Service
{
    private readonly NpgsqlDataSource _dataSource;

    public Service(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<Response> CreateTravelcardAsync(Request request, ILambdaContext context)
    {
        context.Logger.LogLine("DB operation: travelcards INSERT");
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            await using var cmd = new NpgsqlCommand(@"
INSERT INTO public.travelcards
(travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES
(@travelcard_type, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to)
RETURNING id;", connection, transaction);

            cmd.Parameters.Add(new NpgsqlParameter("travelcard_type", request.TravelcardType));
            cmd.Parameters.Add(new NpgsqlParameter("travelcard_valid_from", request.TravelcardValidFrom));
            cmd.Parameters.Add(new NpgsqlParameter("travelcard_valid_to", request.TravelcardValidTo));
            cmd.Parameters.Add(new NpgsqlParameter("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("travelcard_number", request.TravelcardNumber));
            cmd.Parameters.Add(new NpgsqlParameter("travelcard_requested_date", request.TravelcardRequestedDate));
            cmd.Parameters.Add(new NpgsqlParameter("travelcard_transaction_reference", request.TravelcardTransactionReference));
            cmd.Parameters.Add(new NpgsqlParameter("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value));

            var travelcardId = (int)(await cmd.ExecuteScalarAsync())!;

            context.Logger.LogLine("DB success: travelcards generated ID=" + travelcardId);

            context.Logger.LogLine("DB operation: cardholders INSERT");
            foreach (var cardholder in request.Cardholders)
            {
                await using var chCmd = new NpgsqlCommand(@"
INSERT INTO public.cardholders
(travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES
(@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);", connection, transaction);

                chCmd.Parameters.Add(new NpgsqlParameter("travelcard_id", travelcardId));
                chCmd.Parameters.Add(new NpgsqlParameter("cardholder_title", cardholder.CardholderTitle));
                chCmd.Parameters.Add(new NpgsqlParameter("cardholder_forename", cardholder.CardholderForename));
                chCmd.Parameters.Add(new NpgsqlParameter("cardholder_surname", cardholder.CardholderSurname));
                chCmd.Parameters.Add(new NpgsqlParameter("cardholder_type", cardholder.CardholderType));
                chCmd.Parameters.Add(new NpgsqlParameter("cardholder_photo_name", cardholder.CardholderPhotoName));
                chCmd.Parameters.Add(new NpgsqlParameter("cardholder_photo_rrs_key", (object?)cardholder.CardholderPhotoRRSKey ?? DBNull.Value));
                chCmd.Parameters.Add(new NpgsqlParameter("cardholder_photo_url", (object?)cardholder.CardholderPhotoURL ?? DBNull.Value));
                chCmd.Parameters.Add(new NpgsqlParameter("cardholder_photo_key", (object?)cardholder.CardholderPhotoKey ?? DBNull.Value));

                await chCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();

            return new Response
            {
                TravelcardId = Guid.NewGuid().ToString(),
                Token = Path.GetRandomFileName().Replace(".", string.Empty)[..6].ToUpperInvariant()
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}