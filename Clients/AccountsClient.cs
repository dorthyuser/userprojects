using Npgsql;
using NpgsqlTypes;

namespace BusTravelAccountsSaLambda;

public sealed class AccountsClient
{
    private readonly Settings _settings;

    public AccountsClient(Settings settings)
    {
        _settings = settings;
    }

    public async Task CheckReadyAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(BuildConnectionString(await _settings.GetDatabaseSettingsAsync()));
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("select 1", connection);
        try
        {
            await command.ExecuteScalarAsync(cancellationToken);
            AppLog.Info("PostgreSQL QUERY select 1 succeeded");
        }
        catch (PostgresException ex)
        {
            AppLog.Error("PostgreSQL readiness query failed", ex);
            throw new PostgresOperationException(ex.SqlState ?? "00000", ex.MessageText, ex);
        }
    }

    public async Task<List<AccountRow>> GetAccountsByEmailAsync(string email, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(BuildConnectionString(await _settings.GetDatabaseSettingsAsync()));
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(@"select id, salutation, first_name, last_name, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted, person_email from public.sa_accounts where lower(person_email) = lower(@email)", connection);
        command.Parameters.AddWithValue("email", NpgsqlDbType.Text, email ?? string.Empty);
        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var rows = new List<AccountRow>();
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(AccountRow.FromReader(reader));
            }

            AppLog.Info($"PostgreSQL QUERY public.sa_accounts rows={rows.Count}");
            return rows;
        }
        catch (PostgresException ex)
        {
            AppLog.Error("PostgreSQL query public.sa_accounts failed", ex);
            throw new PostgresOperationException(ex.SqlState ?? "00000", ex.MessageText, ex);
        }
    }

    public async Task<AccountRow?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(BuildConnectionString(await _settings.GetDatabaseSettingsAsync()));
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(@"select id, salutation, first_name, last_name, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted, person_email from public.sa_accounts where id = @id", connection);
        command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, id);
        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                AppLog.Info("PostgreSQL QUERY public.sa_accounts rows=0");
                return null;
            }

            AppLog.Info("PostgreSQL QUERY public.sa_accounts rows=1");
            return AccountRow.FromReader(reader);
        }
        catch (PostgresException ex)
        {
            AppLog.Error("PostgreSQL query public.sa_accounts by id failed", ex);
            throw new PostgresOperationException(ex.SqlState ?? "00000", ex.MessageText, ex);
        }
    }

    public async Task<Guid> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var settings = await _settings.GetDatabaseSettingsAsync();
        await using var connection = new NpgsqlConnection(BuildConnectionString(settings));
        await connection.OpenAsync(cancellationToken);
        var id = Guid.NewGuid();
        await using var command = new NpgsqlCommand(@"insert into public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted, created_at, updated_at) values (@id, @salutation, @first_name, @last_name, @person_email, @person_birthdate, @phone, @mobile_phone, @mailing_street, @mailing_postal_code, @mailing_city, @mailing_country, @account_status, @account_source, coalesce(@hotlisted, false), now(), now()) returning id", connection);
        AddCreateParameters(command, id, request);
        try
        {
            var result = await command.ExecuteScalarAsync(cancellationToken);
            var createdId = result is Guid g ? g : id;
            AppLog.Info("PostgreSQL INSERT public.sa_accounts rows=1");
            return createdId;
        }
        catch (PostgresException ex)
        {
            AppLog.Error("PostgreSQL insert public.sa_accounts failed", ex);
            throw new PostgresOperationException(ex.SqlState ?? "00000", ex.MessageText, ex);
        }
    }

    public async Task<bool> UpdateAccountAsync(Guid id, UpdateAccountRequest request, CancellationToken cancellationToken)
    {
        var settings = await _settings.GetDatabaseSettingsAsync();
        await using var connection = new NpgsqlConnection(BuildConnectionString(settings));
        await connection.OpenAsync(cancellationToken);
        var sets = new List<string> { "updated_at = now()" };
        var command = new NpgsqlCommand();
        command.Connection = connection;
        command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, id);
        AddUpdateParameters(command, request, sets);
        command.CommandText = $"update public.sa_accounts set {string.Join(", ", sets)} where id = @id";
        try
        {
            var rows = await command.ExecuteNonQueryAsync(cancellationToken);
            AppLog.Info($"PostgreSQL UPDATE public.sa_accounts rows={rows}");
            return rows > 0;
        }
        catch (PostgresException ex)
        {
            AppLog.Error("PostgreSQL update public.sa_accounts failed", ex);
            throw new PostgresOperationException(ex.SqlState ?? "00000", ex.MessageText, ex);
        }
    }

    private static string BuildConnectionString(DatabaseSettings settings) => new NpgsqlConnectionStringBuilder
    {
        Host = settings.Host,
        Port = settings.Port,
        Database = settings.Database,
        Username = settings.Username,
        Password = settings.Password,
        SslMode = SslMode.Require,
        TrustServerCertificate = false
    }.ConnectionString;

    private static void AddCreateParameters(NpgsqlCommand command, Guid id, CreateAccountRequest request)
    {
        command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, id);
        command.Parameters.AddWithValue("salutation", NpgsqlDbType.Text, (object?)request.Salutation ?? DBNull.Value);
        command.Parameters.AddWithValue("first_name", NpgsqlDbType.Text, (object?)request.FirstName ?? DBNull.Value);
        command.Parameters.AddWithValue("last_name", NpgsqlDbType.Text, (object?)request.LastName ?? DBNull.Value);
        command.Parameters.AddWithValue("person_email", NpgsqlDbType.Text, (object?)request.PersonEmail ?? DBNull.Value);
        command.Parameters.AddWithValue("person_birthdate", NpgsqlDbType.Date, (object?)request.PersonBirthdate ?? DBNull.Value);
        command.Parameters.AddWithValue("phone", NpgsqlDbType.Text, (object?)request.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("mobile_phone", NpgsqlDbType.Text, (object?)request.MobilePhone ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_street", NpgsqlDbType.Text, (object?)request.MailingStreet ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_postal_code", NpgsqlDbType.Text, (object?)request.MailingPostalCode ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_city", NpgsqlDbType.Text, (object?)request.MailingCity ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_country", NpgsqlDbType.Text, (object?)request.MailingCountry ?? DBNull.Value);
        command.Parameters.AddWithValue("account_status", NpgsqlDbType.Text, (object?)request.AccountStatus ?? DBNull.Value);
        command.Parameters.AddWithValue("account_source", NpgsqlDbType.Text, (object?)request.AccountSource ?? DBNull.Value);
        command.Parameters.AddWithValue("hotlisted", NpgsqlDbType.Boolean, (object?)request.Hotlisted ?? DBNull.Value);
    }

    private static void AddUpdateParameters(NpgsqlCommand command, UpdateAccountRequest request, List<string> sets)
    {
        if (request.Salutation is not null) { command.Parameters.AddWithValue("salutation", NpgsqlDbType.Text, request.Salutation); sets.Add("salutation = @salutation"); }
        if (request.FirstName is not null) { command.Parameters.AddWithValue("first_name", NpgsqlDbType.Text, request.FirstName); sets.Add("first_name = @first_name"); }
        if (request.LastName is not null) { command.Parameters.AddWithValue("last_name", NpgsqlDbType.Text, request.LastName); sets.Add("last_name = @last_name"); }
        if (request.PersonEmail is not null) { command.Parameters.AddWithValue("person_email", NpgsqlDbType.Text, request.PersonEmail); sets.Add("person_email = @person_email"); }
        if (request.PersonBirthdate.HasValue) { command.Parameters.AddWithValue("person_birthdate", NpgsqlDbType.Date, request.PersonBirthdate.Value); sets.Add("person_birthdate = @person_birthdate"); }
        if (request.Phone is not null) { command.Parameters.AddWithValue("phone", NpgsqlDbType.Text, request.Phone); sets.Add("phone = @phone"); }
        if (request.MobilePhone is not null) { command.Parameters.AddWithValue("mobile_phone", NpgsqlDbType.Text, request.MobilePhone); sets.Add("mobile_phone = @mobile_phone"); }
        if (request.MailingStreet is not null) { command.Parameters.AddWithValue("mailing_street", NpgsqlDbType.Text, request.MailingStreet); sets.Add("mailing_street = @mailing_street"); }
        if (request.MailingPostalCode is not null) { command.Parameters.AddWithValue("mailing_postal_code", NpgsqlDbType.Text, request.MailingPostalCode); sets.Add("mailing_postal_code = @mailing_postal_code"); }
        if (request.MailingCity is not null) { command.Parameters.AddWithValue("mailing_city", NpgsqlDbType.Text, request.MailingCity); sets.Add("mailing_city = @mailing_city"); }
        if (request.MailingCountry is not null) { command.Parameters.AddWithValue("mailing_country", NpgsqlDbType.Text, request.MailingCountry); sets.Add("mailing_country = @mailing_country"); }
        if (request.AccountStatus is not null) { command.Parameters.AddWithValue("account_status", NpgsqlDbType.Text, request.AccountStatus); sets.Add("account_status = @account_status"); }
        if (request.AccountSource is not null) { command.Parameters.AddWithValue("account_source", NpgsqlDbType.Text, request.AccountSource); sets.Add("account_source = @account_source"); }
        if (request.Hotlisted.HasValue) { command.Parameters.AddWithValue("hotlisted", NpgsqlDbType.Boolean, request.Hotlisted.Value); sets.Add("hotlisted = @hotlisted"); }
    }
}
