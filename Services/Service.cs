using System.Text.Json;
using Npgsql;
using DemoshauntcLambda.Models;

namespace DemoshauntcLambda.Services;

public sealed class Service
{
    private readonly NpgsqlDataSource _dataSource;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(null, allowIntegerValues: false) }
    };

    public Service(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<Response> CreateAsync(Request request)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();

        const string sql = @"INSERT INTO travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES (@travelcard_type, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to)
RETURNING id;";

        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.Add(new NpgsqlParameter("travelcard_type", request.TravelcardType));
        cmd.Parameters.Add(new NpgsqlParameter("travelcard_valid_from", request.TravelcardValidFrom));
        cmd.Parameters.Add(new NpgsqlParameter("travelcard_valid_to", request.TravelcardValidTo));
        cmd.Parameters.Add(new NpgsqlParameter("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value));
        cmd.Parameters.Add(new NpgsqlParameter("travelcard_number", request.TravelcardNumber));
        cmd.Parameters.Add(new NpgsqlParameter("travelcard_requested_date", request.TravelcardRequestedDate));
        cmd.Parameters.Add(new NpgsqlParameter("travelcard_transaction_reference", request.TravelcardTransactionReference));
        cmd.Parameters.Add(new NpgsqlParameter("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value));

        var travelcardId = (int)(await cmd.ExecuteScalarAsync())!;

        foreach (var ch in request.Cardholders)
        {
            const string chSql = @"INSERT INTO cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";
            await using var chCmd = new NpgsqlCommand(chSql, conn, tx);
            chCmd.Parameters.Add(new NpgsqlParameter("travelcard_id", travelcardId));
            chCmd.Parameters.Add(new NpgsqlParameter("cardholder_title", ch.CardholderTitle));
            chCmd.Parameters.Add(new NpgsqlParameter("cardholder_forename", ch.CardholderForename));
            chCmd.Parameters.Add(new NpgsqlParameter("cardholder_surname", ch.CardholderSurname));
            chCmd.Parameters.Add(new NpgsqlParameter("cardholder_type", ch.CardholderType));
            chCmd.Parameters.Add(new NpgsqlParameter("cardholder_photo_name", ch.CardholderPhotoName));
            chCmd.Parameters.Add(new NpgsqlParameter("cardholder_photo_rrs_key", (object?)ch.CardholderPhotoRRSKey ?? DBNull.Value));
            chCmd.Parameters.Add(new NpgsqlParameter("cardholder_photo_url", (object?)ch.CardholderPhotoURL ?? DBNull.Value));
            chCmd.Parameters.Add(new NpgsqlParameter("cardholder_photo_key", (object?)ch.CardholderPhotoKey ?? DBNull.Value));
            await chCmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
        return new Response { TravelcardId = travelcardId.ToString(), Token = "P5SSY6" };
    }
}