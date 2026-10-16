using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace Azfcsharp8Function;

public sealed class PostgreSqlClient
{
    private static readonly NpgsqlDataSource DataSource;

    static PostgreSqlClient()
    {
        var settings = new Settings();
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = settings.PostgresHost,
            Port = settings.PostgresPort,
            Database = settings.PostgresDatabase,
            Username = settings.PostgresUsername,
            Password = settings.PostgresPassword,
            SslMode = SslMode.Require,
            TrustServerCertificate = false
        };
        DataSource = NpgsqlDataSource.Create(builder.ConnectionString);
    }

    public async Task<List<AccountResponse>> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        const string sql = @"select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where lower(person_email) = lower(@email)";
        AppLog.Info("PostgreSQL GET /accounts started");
        try
        {
            await using var cmd = DataSource.CreateCommand(sql);
            cmd.Parameters.AddWithValue("email", email);
            var list = new List<AccountResponse>();
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(Map(reader));
            }
            AppLog.Info($"PostgreSQL GET /accounts completed with {list.Count} rows");
            return list;
        }
        catch (Exception ex)
        {
            AppLog.Error("PostgreSQL GET /accounts failed", ex);
            throw;
        }
    }

    public async Task<AccountResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = @"select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where id = @id";
        AppLog.Info("PostgreSQL GET /accounts/{id} started");
        try
        {
            await using var cmd = DataSource.CreateCommand(sql);
            cmd.Parameters.AddWithValue("id", id);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            var result = await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
            AppLog.Info(result is null ? "PostgreSQL GET /accounts/{id} completed with 0 rows" : "PostgreSQL GET /accounts/{id} completed with 1 row");
            return result;
        }
        catch (Exception ex)
        {
            AppLog.Error("PostgreSQL GET /accounts/{id} failed", ex);
            throw;
        }
    }

    public async Task<string> InsertAsync(AccountRequest request, CancellationToken cancellationToken)
    {
        const string sql = @"insert into public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source) values (gen_random_uuid(), @salutation, @first_name, @last_name, @person_email, @person_birthdate, @phone, @mobile_phone, @mailing_street, @mailing_postal_code, @mailing_city, @mailing_country, @account_status, @account_source) returning id";
        AppLog.Info("PostgreSQL INSERT public.sa_accounts started");
        try
        {
            await using var cmd = DataSource.CreateCommand(sql);
            AddCommonParameters(cmd, request);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            var id = reader.GetGuid(0).ToString();
            AppLog.Info("PostgreSQL INSERT public.sa_accounts completed with 1 row");
            return id;
        }
        catch (Exception ex)
        {
            AppLog.Error("PostgreSQL INSERT public.sa_accounts failed", ex);
            throw;
        }
    }

    public async Task<string?> UpdateAsync(Guid id, AccountRequest request, CancellationToken cancellationToken)
    {
        const string sql = @"update public.sa_accounts set salutation = @salutation, first_name = @first_name, last_name = @last_name, person_email = @person_email, phone = coalesce(@phone, phone), mobile_phone = coalesce(@mobile_phone, mobile_phone), mailing_street = @mailing_street, mailing_postal_code = @mailing_postal_code, mailing_city = @mailing_city, mailing_country = @mailing_country, account_status = @account_status, account_source = @account_source, updated_at = now() where id = @id returning id";
        AppLog.Info("PostgreSQL UPDATE public.sa_accounts started");
        try
        {
            await using var cmd = DataSource.CreateCommand(sql);
            cmd.Parameters.AddWithValue("id", id);
            AddCommonParameters(cmd, request);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            var result = await reader.ReadAsync(cancellationToken) ? reader.GetGuid(0).ToString() : null;
            AppLog.Info(result is null ? "PostgreSQL UPDATE public.sa_accounts completed with 0 rows" : "PostgreSQL UPDATE public.sa_accounts completed with 1 row");
            return result;
        }
        catch (Exception ex)
        {
            AppLog.Error("PostgreSQL UPDATE public.sa_accounts failed", ex);
            throw;
        }
    }

    private static void AddCommonParameters(NpgsqlCommand cmd, AccountRequest request)
    {
        cmd.Parameters.AddWithValue("salutation", (object?)AppHelpers.TrimOrNullIfPresent(request.Salutation) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("first_name", (object?)AppHelpers.TrimOrNullIfPresent(request.FirstName) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("last_name", (object?)AppHelpers.TrimOrNullIfPresent(request.LastName) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("person_email", (object?)AppHelpers.TrimOrNullIfPresent(request.PersonEmail) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("person_birthdate", (object?)ParseDate(request.PersonBirthdate) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("phone", (object?)AppHelpers.TrimOrNullIfPresent(request.Phone) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("mobile_phone", (object?)AppHelpers.TrimOrNullIfPresent(request.MobilePhone) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("mailing_street", (object?)AppHelpers.TrimOrNullIfPresent(request.MailingStreet) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("mailing_postal_code", (object?)AppHelpers.TrimOrNullIfPresent(request.MailingPostalCode) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("mailing_city", (object?)AppHelpers.TrimOrNullIfPresent(request.MailingCity) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("mailing_country", (object?)AppHelpers.TrimOrNullIfPresent(request.MailingCountry) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("account_status", (object?)AppHelpers.TrimOrNullIfPresent(request.AccountStatus) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("account_source", (object?)AppHelpers.TrimOrNullIfPresent(request.AccountSource) ?? DBNull.Value);
    }

    private static DateOnly? ParseDate(string? value) => string.IsNullOrWhiteSpace(value) ? null : DateOnly.Parse(value);

    private static AccountResponse Map(NpgsqlDataReader reader) => new()
    {
        id = reader.GetGuid(0).ToString(),
        salutation = reader.IsDBNull(1) ? null : reader.GetString(1).Trim(),
        firstName = reader.IsDBNull(2) ? null : reader.GetString(2).Trim(),
        lastName = reader.IsDBNull(3) ? null : reader.GetString(3).Trim(),
        personEmail = reader.IsDBNull(4) ? null : reader.GetString(4).Trim(),
        personBirthdate = reader.IsDBNull(5) ? null : reader.GetFieldValue<DateOnly>(5).ToString("yyyy-MM-dd"),
        phone = reader.IsDBNull(6) ? null : reader.GetString(6).Trim(),
        mobilePhone = reader.IsDBNull(7) ? null : reader.GetString(7).Trim(),
        mailingStreet = reader.IsDBNull(8) ? null : reader.GetString(8).Trim(),
        mailingPostalCode = reader.IsDBNull(9) ? null : reader.GetString(9).Trim(),
        mailingCity = reader.IsDBNull(10) ? null : reader.GetString(10).Trim(),
        mailingCountry = reader.IsDBNull(11) ? null : reader.GetString(11).Trim(),
        accountStatus = reader.IsDBNull(12) ? null : reader.GetString(12).Trim(),
        accountSource = reader.IsDBNull(13) ? null : reader.GetString(13).Trim(),
        hotlisted = reader.IsDBNull(14) ? null : reader.GetBoolean(14)
    };
}
