using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace BusTravelAccountsSaLambda;

public sealed class PostgresClient
{
    private static readonly HttpClient _ = new();
    private readonly string _connectionString;

    public PostgresClient(Settings settings)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = settings.DbHost,
            Port = settings.DbPort,
            Database = settings.DbName,
            Username = settings.DbUsername,
            Password = settings.DbPassword,
            SslMode = SslMode.Require,
            TrustServerCertificate = false
        };
        _connectionString = builder.ConnectionString;
    }

    public async Task<List<AccountRow>> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where lower(person_email) = lower(@email)";
        command.Parameters.AddWithValue("email", email);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<AccountRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(ReadRow(reader));
        }
        return rows;
    }

    public async Task<AccountRow?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where id = @id";
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadRow(reader) : null;
    }

    public async Task<Guid> InsertAsync(AccountWriteModel model, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"insert into public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted) values (@id, @salutation, @first_name, @last_name, @person_email, @person_birthdate, @phone, @mobile_phone, @mailing_street, @mailing_postal_code, @mailing_city, @mailing_country, @account_status, @account_source, coalesce(@hotlisted, false)) returning id";
        AddParameters(command, model, includeId: true, includeUpdatedAt: false);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return (Guid)result!;
    }

    public async Task<(bool Updated, Guid? Id)> UpdateAsync(Guid id, AccountWriteModel model, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"update public.sa_accounts set salutation = @salutation, first_name = @first_name, last_name = @last_name, person_email = @person_email, person_birthdate = @person_birthdate, phone = @phone, mobile_phone = @mobile_phone, mailing_street = @mailing_street, mailing_postal_code = @mailing_postal_code, mailing_city = @mailing_city, mailing_country = @mailing_country, account_status = @account_status, account_source = @account_source, updated_at = now() where id = @id returning id";
        AddParameters(command, model, includeId: true, includeUpdatedAt: true, id: id);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is Guid guid ? (true, guid) : (false, null);
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "select 1";
            await command.ExecuteScalarAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static AccountRow ReadRow(NpgsqlDataReader reader)
    {
        return new AccountRow
        {
            Id = reader.GetGuid(0),
            Salutation = reader.IsDBNull(1) ? null : reader.GetString(1),
            FirstName = reader.IsDBNull(2) ? null : reader.GetString(2),
            LastName = reader.GetString(3),
            PersonEmail = reader.GetString(4),
            PersonBirthdate = reader.IsDBNull(5) ? null : DateOnly.FromDateTime(reader.GetDateTime(5)),
            Phone = reader.IsDBNull(6) ? null : reader.GetString(6),
            MobilePhone = reader.IsDBNull(7) ? null : reader.GetString(7),
            MailingStreet = reader.IsDBNull(8) ? null : reader.GetString(8),
            MailingPostalCode = reader.IsDBNull(9) ? null : reader.GetString(9),
            MailingCity = reader.IsDBNull(10) ? null : reader.GetString(10),
            MailingCountry = reader.IsDBNull(11) ? null : reader.GetString(11),
            AccountStatus = reader.IsDBNull(12) ? null : reader.GetString(12),
            AccountSource = reader.IsDBNull(13) ? null : reader.GetString(13),
            Hotlisted = reader.GetBoolean(14)
        };
    }

    private static void AddParameters(NpgsqlCommand command, AccountWriteModel model, bool includeId, bool includeUpdatedAt, Guid? id = null)
    {
        if (includeId)
        {
            command.Parameters.AddWithValue("id", id ?? model.Id ?? Guid.NewGuid());
        }
        command.Parameters.AddWithValue("salutation", (object?)model.Salutation ?? DBNull.Value);
        command.Parameters.AddWithValue("first_name", (object?)model.FirstName ?? DBNull.Value);
        command.Parameters.AddWithValue("last_name", model.LastName ?? string.Empty);
        command.Parameters.AddWithValue("person_email", model.PersonEmail ?? string.Empty);
        command.Parameters.AddWithValue("person_birthdate", (object?)model.PersonBirthdate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
        command.Parameters.AddWithValue("phone", (object?)model.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("mobile_phone", (object?)model.MobilePhone ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_street", (object?)model.MailingStreet ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_postal_code", (object?)model.MailingPostalCode ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_city", (object?)model.MailingCity ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_country", (object?)model.MailingCountry ?? DBNull.Value);
        command.Parameters.AddWithValue("account_status", (object?)model.AccountStatus ?? DBNull.Value);
        command.Parameters.AddWithValue("account_source", (object?)model.AccountSource ?? DBNull.Value);
        command.Parameters.AddWithValue("hotlisted", (object?)model.Hotlisted ?? DBNull.Value);
    }
}

public sealed class AccountRow
{
    public Guid Id { get; set; }
    public string? Salutation { get; set; }
    public string? FirstName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string PersonEmail { get; set; } = string.Empty;
    public DateOnly? PersonBirthdate { get; set; }
    public string? Phone { get; set; }
    public string? MobilePhone { get; set; }
    public string? MailingStreet { get; set; }
    public string? MailingPostalCode { get; set; }
    public string? MailingCity { get; set; }
    public string? MailingCountry { get; set; }
    public string? AccountStatus { get; set; }
    public string? AccountSource { get; set; }
    public bool Hotlisted { get; set; }
}

public sealed class AccountWriteModel
{
    public Guid? Id { get; set; }
    public string? Salutation { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PersonEmail { get; set; }
    public DateOnly? PersonBirthdate { get; set; }
    public string? Phone { get; set; }
    public string? MobilePhone { get; set; }
    public string? MailingStreet { get; set; }
    public string? MailingPostalCode { get; set; }
    public string? MailingCity { get; set; }
    public string? MailingCountry { get; set; }
    public string? AccountStatus { get; set; }
    public string? AccountSource { get; set; }
    public bool? Hotlisted { get; set; }
}