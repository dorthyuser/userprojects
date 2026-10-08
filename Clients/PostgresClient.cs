using System.Data;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Npgsql;

namespace BusTravelAccountsSaMainLambda;

public sealed class PostgresClient
{
    public async Task<List<AccountRecord>> GetAccountsByEmailAsync(string email, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted
from public.sa_accounts
where lower(person_email) = lower(@email)";
        command.Parameters.AddWithValue("email", email);

        var results = new List<AccountRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapAccountRecord(reader));
        }

        return results;
    }

    public async Task<AccountRecord?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted
from public.sa_accounts
where id = @id";
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapAccountRecord(reader);
        }

        return null;
    }

    public async Task CreateAccountAsync(Guid id, AccountMutationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = @"insert into public.sa_accounts
(id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, created_at, updated_at)
values
(@id, @salutation, @first_name, @last_name, @person_email, @person_birthdate, @phone, @mobile_phone, @mailing_street, @mailing_postal_code, @mailing_city, @mailing_country, @account_status, @account_source, now(), now())";
            AddMutationParameters(command, id, request);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresOperationException)
        {
            throw;
        }
        catch (PostgresException ex)
        {
            throw new PostgresOperationException(ex.SqlState ?? "POSTGRES", ex.MessageText);
        }
    }

    public async Task<bool> UpdateAccountAsync(Guid id, AccountMutationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = @"update public.sa_accounts
set salutation = @salutation,
    first_name = @first_name,
    last_name = @last_name,
    person_email = @person_email,
    person_birthdate = @person_birthdate,
    phone = @phone,
    mobile_phone = @mobile_phone,
    mailing_street = @mailing_street,
    mailing_postal_code = @mailing_postal_code,
    mailing_city = @mailing_city,
    mailing_country = @mailing_country,
    account_status = @account_status,
    account_source = @account_source,
    updated_at = now()
where id = @id";
            AddMutationParameters(command, id, request);
            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            return affected > 0;
        }
        catch (PostgresOperationException)
        {
            throw;
        }
        catch (PostgresException ex)
        {
            throw new PostgresOperationException(ex.SqlState ?? "POSTGRES", ex.MessageText);
        }
    }

    private static void AddMutationParameters(NpgsqlCommand command, Guid id, AccountMutationRequest request)
    {
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("salutation", (object?)request.Salutation ?? DBNull.Value);
        command.Parameters.AddWithValue("first_name", (object?)request.FirstName ?? DBNull.Value);
        command.Parameters.AddWithValue("last_name", (object?)request.LastName ?? DBNull.Value);
        command.Parameters.AddWithValue("person_email", (object?)request.PersonEmail ?? DBNull.Value);
        command.Parameters.AddWithValue("person_birthdate", request.PersonBirthdate.HasValue ? request.PersonBirthdate.Value : DBNull.Value);
        command.Parameters.AddWithValue("phone", (object?)request.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("mobile_phone", (object?)request.MobilePhone ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_street", (object?)request.MailingStreet ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_postal_code", (object?)request.MailingPostalCode ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_city", (object?)request.MailingCity ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_country", (object?)request.MailingCountry ?? DBNull.Value);
        command.Parameters.AddWithValue("account_status", (object?)request.AccountStatus ?? DBNull.Value);
        command.Parameters.AddWithValue("account_source", (object?)request.AccountSource ?? DBNull.Value);
    }

    private static AccountRecord MapAccountRecord(NpgsqlDataReader reader)
    {
        return new AccountRecord
        {
            Id = reader.GetGuid(reader.GetOrdinal("id")),
            Salutation = DbHelper.GetString(reader, "salutation"),
            FirstName = DbHelper.GetString(reader, "first_name"),
            LastName = DbHelper.GetString(reader, "last_name"),
            PersonEmail = DbHelper.GetString(reader, "person_email"),
            PersonBirthdate = DbHelper.GetDateOnly(reader, "person_birthdate"),
            Phone = DbHelper.GetString(reader, "phone"),
            MobilePhone = DbHelper.GetString(reader, "mobile_phone"),
            MailingStreet = DbHelper.GetString(reader, "mailing_street"),
            MailingPostalCode = DbHelper.GetString(reader, "mailing_postal_code"),
            MailingCity = DbHelper.GetString(reader, "mailing_city"),
            MailingCountry = DbHelper.GetString(reader, "mailing_country"),
            AccountStatus = DbHelper.GetString(reader, "account_status"),
            AccountSource = DbHelper.GetString(reader, "account_source"),
            Hotlisted = DbHelper.GetBoolean(reader, "hotlisted")
        };
    }

    private static async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var settings = Settings.Instance;
        var connectionString = await settings.GetConnectionStringAsync(cancellationToken);

        try
        {
            var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch (NpgsqlException ex) when (IsAuthenticationError(ex))
        {
            throw new DatabaseAuthenticationException("Database authentication failed", "Database authentication failed");
        }
        catch (NpgsqlException ex) when (IsConnectivityError(ex))
        {
            throw new DatabaseConnectivityException("Database connectivity failed", "Database connectivity failed");
        }
    }

    private static bool IsAuthenticationError(NpgsqlException ex)
    {
        return ex is PostgresException postgres && postgres.SqlState == "28P01";
    }

    private static bool IsConnectivityError(NpgsqlException ex)
    {
        return ex.InnerException is not null || ex.Message.Contains("connect", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase);
    }
}