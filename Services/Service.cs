using Amazon.Lambda.Core;
using Npgsql;
using TravelcardsDemoLambda.Models;

namespace TravelcardsDemoLambda.Services;

public sealed class Service
{
    private readonly NpgsqlDataSource _dataSource;

    public Service(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<CreateTravelcardResponse> CreateAsync(CreateTravelcardRequest request, ILambdaContext context)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        const string sql = @"INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to) VALUES (@travelcard_type, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to) RETURNING id;";
        await using var cmd = new NpgsqlCommand(sql, connection, transaction);
        cmd.Parameters.Add(new NpgsqlParameter("travelcard_type", request.TravelcardType));
        cmd.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
        cmd.Parameters.AddWithValue("travelcard_valid_to", request.TravelcardValidTo);
        cmd.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("travelcard_number", request.TravelcardNumber);
        cmd.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
        cmd.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference);
        cmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);
        var travelcardId = (int)(await cmd.ExecuteScalarAsync())!;
        foreach (var ch in request.Cardholders)
        {
            const string cardholderSql = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key) VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";
            await using var chCmd = new NpgsqlCommand(cardholderSql, connection, transaction);
            chCmd.Parameters.AddWithValue("travelcard_id", travelcardId);
            chCmd.Parameters.AddWithValue("cardholder_title", ch.CardholderTitle);
            chCmd.Parameters.AddWithValue("cardholder_forename", ch.CardholderForename);
            chCmd.Parameters.AddWithValue("cardholder_surname", ch.CardholderSurname);
            chCmd.Parameters.Add(new NpgsqlParameter("cardholder_type", ch.CardholderType));
            chCmd.Parameters.AddWithValue("cardholder_photo_name", ch.CardholderPhotoName);
            chCmd.Parameters.AddWithValue("cardholder_photo_rrs_key", (object?)ch.CardholderPhotoRRSKey ?? DBNull.Value);
            chCmd.Parameters.AddWithValue("cardholder_photo_url", (object?)ch.CardholderPhotoURL ?? DBNull.Value);
            chCmd.Parameters.AddWithValue("cardholder_photo_key", (object?)ch.CardholderPhotoKey ?? DBNull.Value);
            await chCmd.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
        return new CreateTravelcardResponse { TravelcardId = travelcardId.ToString(), Token = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant() };
    }
}