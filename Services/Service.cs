using System.Text;
using Npgsql;
using Npgsql.NameTranslation;
using Travelcardlambda1010Lambda.Models;

namespace Travelcardlambda1010Lambda.Services;

public sealed class Service
{
    private readonly NpgsqlDataSource _dataSource;

    public Service()
    {
        var builder = new NpgsqlDataSourceBuilder(BuildConnectionString());
        builder.MapEnum<TravelcardType>("travelcard_type_enum", new ExactNameTranslator());
        builder.MapEnum<CardholderType>("cardholder_type_enum", new ExactNameTranslator());
        _dataSource = builder.Build();
    }

    public async Task<TravelcardResponse> CreateTravelcardAsync(TravelcardRequest request)
    {
        Console.WriteLine("Opening database connection");
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            Console.WriteLine("Inserting travelcard record");
            const string travelcardSql = @"INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES (@travelcard_type, @travelcard_valid_from, @travelcard_valid_to, @travelcard_name, @travelcard_number, @travelcard_requested_date, @travelcard_transaction_reference, @travelcard_usable_to)
RETURNING id;";

            await using var travelcardCmd = new NpgsqlCommand(travelcardSql, connection, transaction);
            travelcardCmd.Parameters.Add(new NpgsqlParameter("travelcard_type", request.TravelcardType));
            travelcardCmd.Parameters.AddWithValue("travelcard_valid_from", request.TravelcardValidFrom);
            travelcardCmd.Parameters.AddWithValue("travelcard_valid_to", request.TravelcardValidTo);
            travelcardCmd.Parameters.AddWithValue("travelcard_name", (object?)request.TravelcardName ?? DBNull.Value);
            travelcardCmd.Parameters.AddWithValue("travelcard_number", request.TravelcardNumber);
            travelcardCmd.Parameters.AddWithValue("travelcard_requested_date", request.TravelcardRequestedDate);
            travelcardCmd.Parameters.AddWithValue("travelcard_transaction_reference", request.TravelcardTransactionReference);
            travelcardCmd.Parameters.AddWithValue("travelcard_usable_to", (object?)request.TravelcardUsableTo ?? DBNull.Value);

            var travelcardId = Convert.ToInt32(await travelcardCmd.ExecuteScalarAsync());

            foreach (var holder in request.Cardholders)
            {
                Console.WriteLine($"Inserting cardholder {holder.CardholderType}");
                const string cardholderSql = @"INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcard_id, @cardholder_title, @cardholder_forename, @cardholder_surname, @cardholder_type, @cardholder_photo_name, @cardholder_photo_rrs_key, @cardholder_photo_url, @cardholder_photo_key);";

                await using var cardholderCmd = new NpgsqlCommand(cardholderSql, connection, transaction);
                cardholderCmd.Parameters.AddWithValue("travelcard_id", travelcardId);
                cardholderCmd.Parameters.AddWithValue("cardholder_title", holder.CardholderTitle);
                cardholderCmd.Parameters.AddWithValue("cardholder_forename", holder.CardholderForename);
                cardholderCmd.Parameters.AddWithValue("cardholder_surname", holder.CardholderSurname);
                cardholderCmd.Parameters.Add(new NpgsqlParameter("cardholder_type", holder.CardholderType));
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_name", holder.CardholderPhotoName);
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_rrs_key", (object?)holder.CardholderPhotoRRSKey ?? DBNull.Value);
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_url", (object?)holder.CardholderPhotoURL ?? DBNull.Value);
                cardholderCmd.Parameters.AddWithValue("cardholder_photo_key", (object?)holder.CardholderPhotoKey ?? DBNull.Value);
                await cardholderCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return new TravelcardResponse { TravelcardId = Guid.NewGuid().ToString(), Token = GenerateToken() };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database error: {ex}");
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static string BuildConnectionString()
    {
        var host = SecretsHelper.Get("host", "POSTGRESQLHOST");
        var port = SecretsHelper.Get("port", "POSTGRESQLPORT");
        var database = SecretsHelper.Get("dbname", "POSTGRESQLDATABASE");
        var username = SecretsHelper.Get("username", "POSTGRESQLUSERNAME");
        var password = SecretsHelper.Get("password", "POSTGRESQLPASSWORD");

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = int.TryParse(port, out var parsedPort) ? parsedPort : 5432,
            Database = database,
            Username = username,
            Password = password,
            SslMode = SslMode.Require,
            TrustServerCertificate = true
        };

        return builder.ConnectionString;
    }

    private static string GenerateToken()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var bytes = new byte[6];
        Random.Shared.NextBytes(bytes);
        var sb = new StringBuilder(6);
        for (var i = 0; i < 6; i++)
        {
            sb.Append(chars[bytes[i] % chars.Length]);
        }
        return sb.ToString();
    }
}

public sealed class ExactNameTranslator : INpgsqlNameTranslator
{
    public string TranslateMemberName(string memberName) => memberName;
    public string TranslateTypeName(string typeName) => typeName;
}