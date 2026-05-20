using DemotravelcardLambda.Models;
using Npgsql;

namespace DemotravelcardLambda.Services;

public sealed class Service
{
    private readonly NpgsqlDataSource _dataSource;

    public Service(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<Response> CreateAsync(Request request)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var insertTravelcardSql = @"INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES (@travelcard_type, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to)
RETURNING id;";

        await using var cmd = new NpgsqlCommand(insertTravelcardSql, connection, transaction);
        cmd.Parameters.Add(new NpgsqlParameter("travelcard_type", request.TravelcardType!.Value));
        cmd.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
        cmd.Parameters.AddWithValue("travelcard_valid_to", request.TravelcardValidTo);
        cmd.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("travelcard_number", request.TravelcardNumber ?? string.Empty);
        cmd.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
        cmd.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference ?? string.Empty);
        cmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);

        var travelcardId = (int)(await cmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("Unable to create travelcard."));

        foreach (var c in request.Cardholders)
        {
            var insertCardholderSql = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";
            await using var ccmd = new NpgsqlCommand(insertCardholderSql, connection, transaction);
            ccmd.Parameters.AddWithValue("travelcard_id", travelcardId);
            ccmd.Parameters.AddWithValue("cardholder_title", c.CardholderTitle ?? string.Empty);
            ccmd.Parameters.AddWithValue("cardholder_forename", c.CardholderForename ?? string.Empty);
            ccmd.Parameters.AddWithValue("cardholder_surname", c.CardholderSurname ?? string.Empty);
            ccmd.Parameters.Add(new NpgsqlParameter("cardholder_type", c.CardholderType!.Value));
            ccmd.Parameters.AddWithValue("cardholder_photo_name", c.CardholderPhotoName ?? string.Empty);
            ccmd.Parameters.AddWithValue("cardholder_photo_rrs_key", (object?)c.CardholderPhotoRRSKey ?? DBNull.Value);
            ccmd.Parameters.AddWithValue("cardholder_photo_url", (object?)c.CardholderPhotoURL ?? DBNull.Value);
            ccmd.Parameters.AddWithValue("cardholder_photo_key", (object?)c.CardholderPhotoKey ?? DBNull.Value);
            await ccmd.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
        return new Response { TravelcardId = travelcardId.ToString(), Token = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant() };
    }
}