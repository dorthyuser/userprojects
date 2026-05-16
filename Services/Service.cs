using DemoTravelcardsLambda.Models;
using Npgsql;

namespace DemoTravelcardsLambda.Services;

public sealed class Service
{
    private readonly NpgsqlDataSource _dataSource;

    public Service(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<CreateTravelcardResponse> CreateAsync(CreateTravelcardRequest request)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        const string travelcardSql = @"INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES (@travelcard_type, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to)
RETURNING id;";

        await using var cmd = new NpgsqlCommand(travelcardSql, connection, transaction);
        cmd.Parameters.Add(new NpgsqlParameter("travelcard_type", request.TravelcardType));
        cmd.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
        cmd.Parameters.AddWithValue("travelcard_valid_to", request.TravelcardValidTo);
        cmd.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("travelcard_number", request.TravelcardNumber);
        cmd.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
        cmd.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference);
        cmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);

        var id = (int)(await cmd.ExecuteScalarAsync() ?? 0);

        const string cardholderSql = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";

        foreach (var cardholder in request.Cardholders)
        {
            await using var ch = new NpgsqlCommand(cardholderSql, connection, transaction);
            ch.Parameters.AddWithValue("travelcard_id", id);
            ch.Parameters.AddWithValue("cardholder_title", cardholder.CardholderTitle);
            ch.Parameters.AddWithValue("cardholder_forename", cardholder.CardholderForename);
            ch.Parameters.AddWithValue("cardholder_surname", cardholder.CardholderSurname);
            ch.Parameters.Add(new NpgsqlParameter("cardholder_type", cardholder.CardholderType));
            ch.Parameters.AddWithValue("cardholder_photo_name", cardholder.CardholderPhotoName);
            ch.Parameters.AddWithValue("cardholder_photo_rrs_key", (object?)cardholder.CardholderPhotoRRSKey ?? DBNull.Value);
            ch.Parameters.AddWithValue("cardholder_photo_url", (object?)cardholder.CardholderPhotoURL ?? DBNull.Value);
            ch.Parameters.AddWithValue("cardholder_photo_key", (object?)cardholder.CardholderPhotoKey ?? DBNull.Value);
            await ch.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
        return new CreateTravelcardResponse { TravelcardId = id.ToString(), Token = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant() };
    }
}