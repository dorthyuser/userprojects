using System.Data;
using System.Text.Json;
using Npgsql;

namespace BusTravelAccountsLambda;

public sealed class SaSalesforceClient
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static NpgsqlDataSource? DataSource;
    private static SaDbSettings? CachedSettings;
    private static readonly JsonSerializerOptions JsonOptions = BusTravelAccountsLambda.JsonOptions.Default;

    public async Task<SaApiResult> QueryAccountsByEmailAsync(string email)
    {
        try
        {
            var rows = await QueryAsync("select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where lower(person_email) = lower(@email)", new NpgsqlParameter("email", email));
            if (rows.Count == 0)
            {
                return new SaApiResult { StatusCode = 204, Body = "[]", Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "application/json" } };
            }

            var mapped = rows.Select(MapAccount).ToArray();
            return new SaApiResult { StatusCode = 200, Body = JsonSerializer.Serialize(mapped, JsonOptions), Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "application/json" } };
        }
        catch (Exception ex)
        {
            AppLog.Error("Database error", ex);
            return Error(500, "MULE UNKNOWN ERROR", "An unexpected error occurred");
        }
    }

    public async Task<SaApiResult> QueryAccountByIdAsync(string id)
    {
        try
        {
            var rows = await QueryAsync("select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where id = @id", new NpgsqlParameter("id", Guid.Parse(id)));
            if (rows.Count == 0)
            {
                return new SaApiResult { StatusCode = 204, Body = "[]", Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "application/json" } };
            }

            var mapped = MapAccount(rows[0]);
            return new SaApiResult { StatusCode = 200, Body = JsonSerializer.Serialize(mapped, JsonOptions), Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "application/json" } };
        }
        catch (Exception ex)
        {
            AppLog.Error("Database error", ex);
            return Error(500, "MULE UNKNOWN ERROR", "An unexpected error occurred");
        }
    }

    public async Task<SaApiResult> CreateAccountAsync(SaAccountRequest request)
    {
        try
        {
            var id = Guid.NewGuid();
            var sql = "insert into public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source) values (@id, @salutation, @first_name, @last_name, @person_email, @person_birthdate, @phone, @mobile_phone, @mailing_street, @mailing_postal_code, @mailing_city, @mailing_country, @account_status, @account_source) returning id";
            await ExecuteAsync(sql,
                new NpgsqlParameter("id", id),
                new NpgsqlParameter("salutation", (object?)TrimOrNull(request.Salutation) ?? DBNull.Value),
                new NpgsqlParameter("first_name", (object?)TrimOrNull(request.FirstName) ?? DBNull.Value),
                new NpgsqlParameter("last_name", (object?)TrimOrNull(request.LastName) ?? DBNull.Value),
                new NpgsqlParameter("person_email", (object?)TrimOrNull(request.PersonEmail) ?? DBNull.Value),
                new NpgsqlParameter("person_birthdate", (object?)request.PersonBirthdate ?? DBNull.Value),
                new NpgsqlParameter("phone", (object?)TrimOrNull(request.Phone) ?? DBNull.Value),
                new NpgsqlParameter("mobile_phone", (object?)TrimOrNull(request.MobilePhone) ?? DBNull.Value),
                new NpgsqlParameter("mailing_street", (object?)TrimOrNull(request.MailingStreet) ?? DBNull.Value),
                new NpgsqlParameter("mailing_postal_code", (object?)TrimOrNull(request.MailingPostalCode) ?? DBNull.Value),
                new NpgsqlParameter("mailing_city", (object?)TrimOrNull(request.MailingCity) ?? DBNull.Value),
                new NpgsqlParameter("mailing_country", (object?)TrimOrNull(request.MailingCountry) ?? DBNull.Value),
                new NpgsqlParameter("account_status", (object?)TrimOrNull(request.AccountStatus) ?? DBNull.Value),
                new NpgsqlParameter("account_source", (object?)TrimOrNull(request.AccountSource) ?? DBNull.Value));
            return new SaApiResult { StatusCode = 200, Body = JsonSerializer.Serialize(new { id }, JsonOptions), Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "application/json" } };
        }
        catch (Exception ex)
        {
            AppLog.Error("Database error", ex);
            return Error(500, "MULE UNKNOWN ERROR", "An unexpected error occurred");
        }
    }

    public async Task<SaApiResult> UpdateAccountAsync(string id, SaAccountRequest request)
    {
        try
        {
            var sql = "update public.sa_accounts set salutation = @salutation, first_name = @first_name, last_name = @last_name, person_email = @person_email, person_birthdate = coalesce(@person_birthdate, person_birthdate), phone = coalesce(@phone, phone), mobile_phone = coalesce(@mobile_phone, mobile_phone), mailing_street = @mailing_street, mailing_postal_code = @mailing_postal_code, mailing_city = @mailing_city, mailing_country = @mailing_country, account_status = @account_status, account_source = @account_source, updated_at = now() where id = @id returning id";
            await ExecuteAsync(sql,
                new NpgsqlParameter("id", Guid.Parse(id)),
                new NpgsqlParameter("salutation", (object?)TrimOrNull(request.Salutation) ?? DBNull.Value),
                new NpgsqlParameter("first_name", (object?)TrimOrNull(request.FirstName) ?? DBNull.Value),
                new NpgsqlParameter("last_name", (object?)TrimOrNull(request.LastName) ?? DBNull.Value),
                new NpgsqlParameter("person_email", (object?)TrimOrNull(request.PersonEmail) ?? DBNull.Value),
                new NpgsqlParameter("person_birthdate", (object?)request.PersonBirthdate ?? DBNull.Value),
                new NpgsqlParameter("phone", (object?)TrimOrNull(request.Phone) ?? DBNull.Value),
                new NpgsqlParameter("mobile_phone", (object?)TrimOrNull(request.MobilePhone) ?? DBNull.Value),
                new NpgsqlParameter("mailing_street", (object?)TrimOrNull(request.MailingStreet) ?? DBNull.Value),
                new NpgsqlParameter("mailing_postal_code", (object?)TrimOrNull(request.MailingPostalCode) ?? DBNull.Value),
                new NpgsqlParameter("mailing_city", (object?)TrimOrNull(request.MailingCity) ?? DBNull.Value),
                new NpgsqlParameter("mailing_country", (object?)TrimOrNull(request.MailingCountry) ?? DBNull.Value),
                new NpgsqlParameter("account_status", (object?)TrimOrNull(request.AccountStatus) ?? DBNull.Value),
                new NpgsqlParameter("account_source", (object?)TrimOrNull(request.AccountSource) ?? DBNull.Value));
            return new SaApiResult { StatusCode = 200, Body = JsonSerializer.Serialize(new { id = Guid.Parse(id) }, JsonOptions), Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "application/json" } };
        }
        catch (Exception ex)
        {
            AppLog.Error("Database error", ex);
            return Error(500, "MULE UNKNOWN ERROR", "An unexpected error occurred");
        }
    }

    private static SaApiResult Error(int statusCode, string message, string description)
    {
        var body = JsonSerializer.Serialize(new ErrorBody { Error = new ErrorDetail { ErrorCode = statusCode, ErrorDateTime = DateTimeOffset.UtcNow, ErrorMessage = message, ErrorDescription = description } }, JsonOptions);
        return new SaApiResult { StatusCode = statusCode, Body = body, Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "application/json" } };
    }

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AccountDto MapAccount(IReadOnlyDictionary<string, object?> row) => new()
    {
        Id = row["id"] is Guid g ? g.ToString() : row["id"]?.ToString() ?? string.Empty,
        Salutation = row["salutation"]?.ToString(),
        FirstName = row["first_name"]?.ToString(),
        LastName = row["last_name"]?.ToString() ?? string.Empty,
        PersonEmail = row["person_email"]?.ToString() ?? string.Empty,
        PersonBirthdate = row["person_birthdate"] is DateTime dt ? DateOnly.FromDateTime(dt) : null,
        Phone = row["phone"]?.ToString(),
        MobilePhone = row["mobile_phone"]?.ToString(),
        MailingStreet = row["mailing_street"]?.ToString(),
        MailingPostalCode = row["mailing_postal_code"]?.ToString(),
        MailingCity = row["mailing_city"]?.ToString(),
        MailingCountry = row["mailing_country"]?.ToString(),
        AccountStatus = row["account_status"]?.ToString(),
        AccountSource = row["account_source"]?.ToString() ?? string.Empty,
        Hotlisted = row["hotlisted"] is bool b && b
    };

    private async Task<List<Dictionary<string, object?>>> QueryAsync(string sql, params NpgsqlParameter[] parameters)
    {
        var dataSource = await GetDataSourceAsync();
        await using var cmd = dataSource.CreateCommand(sql);
        cmd.Parameters.AddRange(parameters);
        await using var reader = await cmd.ExecuteReaderAsync();
        var rows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
            }
            rows.Add(row);
        }
        return rows;
    }

    private async Task ExecuteAsync(string sql, params NpgsqlParameter[] parameters)
    {
        var dataSource = await GetDataSourceAsync();
        await using var cmd = dataSource.CreateCommand(sql);
        cmd.Parameters.AddRange(parameters);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<NpgsqlDataSource> GetDataSourceAsync()
    {
        if (DataSource is not null)
        {
            return DataSource;
        }

        await Gate.WaitAsync();
        try
        {
            if (DataSource is not null)
            {
                return DataSource;
            }

            CachedSettings = SaSettings.Instance.Db;
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = CachedSettings.Host,
                Username = CachedSettings.Username,
                Password = CachedSettings.Password,
                Database = CachedSettings.Database,
                Port = CachedSettings.Port
            };
            DataSource = new NpgsqlDataSourceBuilder(builder.ConnectionString).Build();
            return DataSource;
        }
        finally
        {
            Gate.Release();
        }
    }
}