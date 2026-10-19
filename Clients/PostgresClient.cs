using Npgsql;
using NpgsqlTypes;

namespace BusTravelAccountsSaLambda;

public sealed class PostgresClient
{
    private static readonly SemaphoreSlim Sync = new(1, 1);
    private static NpgsqlDataSource? _dataSource;

    public async Task<IReadOnlyList<AccountResponse>> GetAccountsByEmailAsync(string email, CancellationToken cancellationToken)
    {
        await using var connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand(@"select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where lower(person_email) = lower(@email)", connection);
        command.Parameters.AddWithValue("email", NpgsqlDbType.Text, email);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var results = new List<AccountResponse>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(Map(reader));
        }

        AppLog.Info($"Database GET /accounts outcome rows={results.Count}");
        return results;
    }

    public async Task<AccountResponse?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand(@"select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where id = @id", connection);
        command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var result = Map(reader);
            AppLog.Info("Database GET /accounts/{id} outcome rows=1");
            return result;
        }

        AppLog.Info("Database GET /accounts/{id} outcome rows=0");
        return null;
    }

    public async Task<string> InsertAccountAsync(AccountRequest request, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        await using var connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand(@"insert into public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted, updated_at) values (@id, @salutation, @first_name, @last_name, @person_email, @person_birthdate, @phone, @mobile_phone, @mailing_street, @mailing_postal_code, @mailing_city, @mailing_country, @account_status, @account_source, coalesce(@hotlisted, false), now())", connection);
        AddParameters(command, id, request, includeId: true);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        AppLog.Info($"Database INSERT public.sa_accounts outcome rows={rows}");
        return id.ToString();
    }

    public async Task<string> UpdateAccountAsync(Guid id, AccountRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand(@"update public.sa_accounts set salutation = @salutation, first_name = @first_name, last_name = @last_name, person_email = @person_email, person_birthdate = coalesce(@person_birthdate, person_birthdate), phone = coalesce(@phone, phone), mobile_phone = coalesce(@mobile_phone, mobile_phone), mailing_street = @mailing_street, mailing_postal_code = @mailing_postal_code, mailing_city = @mailing_city, mailing_country = @mailing_country, account_status = @account_status, account_source = @account_source, updated_at = now() where id = @id", connection);
        AddParameters(command, id, request, includeId: true);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        AppLog.Info($"Database UPDATE public.sa_accounts outcome rows={rows}");
        return id.ToString();
    }

    private static void AddParameters(NpgsqlCommand command, Guid id, AccountRequest request, bool includeId)
    {
        if (includeId)
        {
            command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, id);
        }

        command.Parameters.AddWithValue("salutation", NpgsqlDbType.Text, (object?)TrimOrNull(request.Salutation) ?? DBNull.Value);
        command.Parameters.AddWithValue("first_name", NpgsqlDbType.Text, (object?)TrimOrNull(request.FirstName) ?? DBNull.Value);
        command.Parameters.AddWithValue("last_name", NpgsqlDbType.Text, (object?)TrimOrNull(request.LastName) ?? DBNull.Value);
        command.Parameters.AddWithValue("person_email", NpgsqlDbType.Text, (object?)TrimOrNull(request.PersonEmail) ?? DBNull.Value);
        command.Parameters.AddWithValue("person_birthdate", NpgsqlDbType.Date, (object?)request.PersonBirthdate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
        command.Parameters.AddWithValue("phone", NpgsqlDbType.Text, (object?)TrimOrNull(request.Phone) ?? DBNull.Value);
        command.Parameters.AddWithValue("mobile_phone", NpgsqlDbType.Text, (object?)TrimOrNull(request.MobilePhone) ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_street", NpgsqlDbType.Text, (object?)TrimOrNull(request.MailingStreet) ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_postal_code", NpgsqlDbType.Text, (object?)TrimOrNull(request.MailingPostalCode) ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_city", NpgsqlDbType.Text, (object?)TrimOrNull(request.MailingCity) ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_country", NpgsqlDbType.Text, (object?)TrimOrNull(request.MailingCountry) ?? DBNull.Value);
        command.Parameters.AddWithValue("account_status", NpgsqlDbType.Text, (object?)TrimOrNull(request.AccountStatus) ?? DBNull.Value);
        command.Parameters.AddWithValue("account_source", NpgsqlDbType.Text, (object?)TrimOrNull(request.AccountSource) ?? DBNull.Value);
        command.Parameters.AddWithValue("hotlisted", NpgsqlDbType.Boolean, false);
    }

    private static string? TrimOrNull(string? value) => value is null ? null : value.Trim();

    private static AccountResponse Map(NpgsqlDataReader reader)
    {
        return new AccountResponse
        {
            Id = reader.GetGuid(0).ToString(),
            Salutation = reader.IsDBNull(1) ? null : reader.GetString(1).Trim(),
            FirstName = reader.IsDBNull(2) ? null : reader.GetString(2).Trim(),
            LastName = reader.IsDBNull(3) ? null : reader.GetString(3).Trim(),
            PersonEmail = reader.IsDBNull(4) ? null : reader.GetString(4).Trim(),
            PersonBirthdate = reader.IsDBNull(5) ? null : DateOnly.FromDateTime(reader.GetDateTime(5)),
            Phone = reader.IsDBNull(6) ? null : reader.GetString(6).Trim(),
            MobilePhone = reader.IsDBNull(7) ? null : reader.GetString(7).Trim(),
            MailingStreet = reader.IsDBNull(8) ? null : reader.GetString(8).Trim(),
            MailingPostalCode = reader.IsDBNull(9) ? null : reader.GetString(9).Trim(),
            MailingCity = reader.IsDBNull(10) ? null : reader.GetString(10).Trim(),
            MailingCountry = reader.IsDBNull(11) ? null : reader.GetString(11).Trim(),
            AccountStatus = reader.IsDBNull(12) ? null : reader.GetString(12).Trim(),
            AccountSource = reader.IsDBNull(13) ? string.Empty : reader.GetString(13).Trim(),
            Hotlisted = !reader.IsDBNull(14) && reader.GetBoolean(14)
        };
    }

    private static async Task<NpgsqlConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_dataSource is not null)
        {
            return await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        }

        await Sync.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_dataSource is null)
            {
                var settings = Settings.Current;
                var connectionString = await settings.GetConnectionStringAsync(cancellationToken).ConfigureAwait(false);
                _dataSource = NpgsqlDataSource.Create(connectionString);
            }
        }
        finally
        {
            Sync.Release();
        }

        return await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
    }
}