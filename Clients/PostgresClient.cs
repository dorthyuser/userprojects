using System.Data;
using Npgsql;

namespace BusTravelAccountsSaLambda;

public sealed class PostgresClient
{
    private static readonly object Sync = new();
    private static NpgsqlDataSource? DataSource;
    private static string? ConnectionKey;

    private readonly Settings settings;

    public PostgresClient(Settings settings)
    {
        this.settings = settings;
    }

    private NpgsqlDataSource GetDataSource()
    {
        var db = settings.GetDatabaseSettings();
        var key = $"{db.Username}|{db.Password}|{db.Host}|{db.Database}|{db.Port}";

        if (DataSource is not null && ConnectionKey == key)
        {
            return DataSource;
        }

        lock (Sync)
        {
            if (DataSource is not null && ConnectionKey == key)
            {
                return DataSource;
            }

            DataSource?.Dispose();
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = db.Host,
                Username = db.Username,
                Password = db.Password,
                Database = db.Database,
                Port = db.Port,
                SslMode = SslMode.Require,
                TrustServerCertificate = false
            };
            DataSource = NpgsqlDataSource.Create(builder.ConnectionString);
            ConnectionKey = key;
            return DataSource;
        }
    }

    public async Task<List<AccountResponseDto>> GetAccountsByEmailAsync(string email)
    {
        const string sql = @"select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where lower(person_email) = lower(@email) order by id";
        var results = new List<AccountResponseDto>();
        await using var connection = await GetDataSource().OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("email", email);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(ReadAccount(reader));
        }
        return results;
    }

    public async Task<AccountResponseDto?> GetAccountByIdAsync(Guid id)
    {
        const string sql = @"select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where id = @id";
        await using var connection = await GetDataSource().OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadAccount(reader) : null;
    }

    public async Task<Guid> InsertAccountAsync(AccountWriteDto dto)
    {
        const string sql = @"insert into public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted, created_at, updated_at) values (@id, @salutation, @first_name, @last_name, @person_email, @person_birthdate, @phone, @mobile_phone, @mailing_street, @mailing_postal_code, @mailing_city, @mailing_country, @account_status, @account_source, @hotlisted, now(), now())";
        var id = Guid.NewGuid();
        await using var connection = await GetDataSource().OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        AddWriteParameters(command, id, dto);
        await command.ExecuteNonQueryAsync();
        return id;
    }

    public async Task<bool> UpdateAccountAsync(Guid id, AccountWriteDto dto)
    {
        var setParts = new List<string>
        {
            "salutation = @salutation",
            "first_name = @first_name",
            "last_name = @last_name",
            "person_email = @person_email",
            "mailing_street = @mailing_street",
            "mailing_postal_code = @mailing_postal_code",
            "mailing_city = @mailing_city",
            "mailing_country = @mailing_country",
            "account_status = @account_status",
            "account_source = @account_source",
            "hotlisted = @hotlisted",
            "updated_at = now()"
        };

        if (dto.PersonBirthdate.HasValue)
        {
            setParts.Add("person_birthdate = @person_birthdate");
        }

        if (dto.Phone is not null)
        {
            setParts.Add("phone = @phone");
        }

        if (dto.MobilePhone is not null)
        {
            setParts.Add("mobile_phone = @mobile_phone");
        }

        var sql = $"update public.sa_accounts set {string.Join(", ", setParts)} where id = @id";
        await using var connection = await GetDataSource().OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        AddWriteParameters(command, id, dto);
        var rows = await command.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<bool> CanConnectAsync()
    {
        try
        {
            await using var connection = await GetDataSource().OpenConnectionAsync();
            await connection.OpenAsync();
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Error("Database readiness check failed", ex);
            return false;
        }
    }

    private static void AddWriteParameters(NpgsqlCommand command, Guid id, AccountWriteDto dto)
    {
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("salutation", (object?)dto.Salutation ?? DBNull.Value);
        command.Parameters.AddWithValue("first_name", (object?)dto.FirstName ?? DBNull.Value);
        command.Parameters.AddWithValue("last_name", (object?)dto.LastName ?? DBNull.Value);
        command.Parameters.AddWithValue("person_email", (object?)dto.PersonEmail ?? DBNull.Value);
        command.Parameters.AddWithValue("person_birthdate", (object?)dto.PersonBirthdate ?? DBNull.Value);
        command.Parameters.AddWithValue("phone", (object?)dto.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("mobile_phone", (object?)dto.MobilePhone ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_street", (object?)dto.MailingStreet ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_postal_code", (object?)dto.MailingPostalCode ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_city", (object?)dto.MailingCity ?? DBNull.Value);
        command.Parameters.AddWithValue("mailing_country", (object?)dto.MailingCountry ?? DBNull.Value);
        command.Parameters.AddWithValue("account_status", (object?)dto.AccountStatus ?? DBNull.Value);
        command.Parameters.AddWithValue("account_source", (object?)dto.AccountSource ?? DBNull.Value);
        command.Parameters.AddWithValue("hotlisted", dto.Hotlisted);
    }

    private static AccountResponseDto ReadAccount(NpgsqlDataReader reader)
    {
        return new AccountResponseDto
        {
            Id = reader.GetGuid(0),
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
            Hotlisted = reader.GetBoolean(14)
        };
    }
}
