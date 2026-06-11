using System;
using System.Threading.Tasks;
using Npgsql;
using create_travelcard_prod.Models;

namespace create_travelcard_prod.Helpers;

public class TravelcardRepository
{
    private readonly DbHelper _dbHelper;

    public TravelcardRepository(DbHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }

    public async Task<CreateTravelcardResponse> CreateAsync(CreateTravelcardRequest request, string clientId)
    {
        await using var connection = await _dbHelper.DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var travelcardId = Guid.NewGuid();
        var token = "";

        const string sql = @"
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
    @travelcard_type::travelcard_type_enum,
    @travelcard_valid_from,
    @travelcard_valid_to,
    @travelcard_name,
    @travelcard_number,
    @travelcard_requested_date,
    @travelcard_transaction_reference,
    @travelcard_usable_to
) RETURNING id;";

        await using (var cmd = new NpgsqlCommand(sql, connection, transaction))
        {
            cmd.Parameters.AddWithValue("travelcard_type", request.TravelcardType.ToString().ToUpperInvariant());
            cmd.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
            cmd.Parameters.AddWithValue("travelcard_valid_to", (object?)request.TravelcardValidTo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("travelcard_number", request.TravelcardNumber);
            cmd.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
            cmd.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference);
            cmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);
            await cmd.ExecuteScalarAsync();
        }

        await transaction.CommitAsync();
        return new CreateTravelcardResponse { TravelcardId = travelcardId, Token = token };
    }
}
