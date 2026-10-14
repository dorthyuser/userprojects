using System.Data;
using System.Text.Json;
using Npgsql;

namespace BusTravelAccountsLambda;

public sealed class SaPostgresClient
{
    private static readonly string ConnectionString = BuildConnectionString();
    private static string BuildConnectionString()
    {
        var s = SaSettings.Current();
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = s.DbHost ?? string.Empty,
            Port = s.DbPort ?? 5432,
            Database = s.DbName ?? string.Empty,
            Username = s.DbUsername ?? string.Empty,
            Password = s.DbPassword ?? string.Empty,
            SslMode = SslMode.Require,
            TrustServerCertificate = false
        };
        return builder.ConnectionString;
    }

    private static string MapSqlState(string sqlState) => string.IsNullOrWhiteSpace(sqlState) ? "MULE UNKNOWN ERROR" : $"{sqlState} ERROR";

    public async Task<SaApiResult> GetAccountsAsync(string? email)
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync(CancellationToken.None);
            await using var cmd = new NpgsqlCommand(@"select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where lower(person_email) = lower(@email)", conn);
            cmd.Parameters.AddWithValue("email", (object?)email ?? DBNull.Value);
            await using var reader = await cmd.ExecuteReaderAsync(CancellationToken.None);
            var rows = new List<object>();
            while (await reader.ReadAsync(CancellationToken.None))
            {
                rows.Add(new
                {
                    id = reader.GetGuid(0),
                    salutation = reader.IsDBNull(1) ? null : reader.GetString(1),
                    firstName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    lastName = reader.IsDBNull(3) ? null : reader.GetString(3),
                    personEmail = reader.IsDBNull(4) ? null : reader.GetString(4),
                    personBirthdate = reader.IsDBNull(5) ? (DateOnly?)null : DateOnly.FromDateTime(reader.GetDateTime(5)),
                    phone = reader.IsDBNull(6) ? null : reader.GetString(6),
                    mobilePhone = reader.IsDBNull(7) ? null : reader.GetString(7),
                    mailingStreet = reader.IsDBNull(8) ? null : reader.GetString(8),
                    mailingPostalCode = reader.IsDBNull(9) ? null : reader.GetString(9),
                    mailingCity = reader.IsDBNull(10) ? null : reader.GetString(10),
                    mailingCountry = reader.IsDBNull(11) ? null : reader.GetString(11),
                    accountStatus = reader.IsDBNull(12) ? null : reader.GetString(12),
                    accountSource = reader.IsDBNull(13) ? null : reader.GetString(13),
                    hotlisted = reader.GetBoolean(14)
                });
            }
            return new SaApiResult { StatusCode = rows.Count == 0 ? 204 : 200, Body = JsonSerializer.Serialize(rows, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
        catch (PostgresException ex)
        {
            AppLog.Error("Database error", ex);
            return new SaApiResult { StatusCode = 400, Body = JsonSerializer.Serialize(new SaErrorResponse { Error = new SaErrorBody { ErrorCode = 400, ErrorDateTime = DateTime.UtcNow, ErrorMessage = MapSqlState(ex.SqlState), ErrorDescription = ex.MessageText } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return new SaApiResult { StatusCode = 500, Body = JsonSerializer.Serialize(new SaErrorResponse { Error = new SaErrorBody { ErrorCode = 500, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "MULE UNKNOWN ERROR", ErrorDescription = "An unexpected error occurred" } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
    }

    public async Task<SaApiResult> GetAccountDetailsAsync(string id)
    {
        try
        {
            if (!Guid.TryParse(id, out var guid)) return new SaApiResult { StatusCode = 204, Body = "[]", Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync(CancellationToken.None);
            await using var cmd = new NpgsqlCommand(@"select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where id = @id", conn);
            cmd.Parameters.AddWithValue("id", guid);
            await using var reader = await cmd.ExecuteReaderAsync(CancellationToken.None);
            if (!await reader.ReadAsync(CancellationToken.None)) return new SaApiResult { StatusCode = 204, Body = "[]", Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
            var body = new
            {
                id = reader.GetGuid(0),
                salutation = reader.IsDBNull(1) ? null : reader.GetString(1),
                firstName = reader.IsDBNull(2) ? null : reader.GetString(2),
                lastName = reader.IsDBNull(3) ? null : reader.GetString(3),
                personEmail = reader.IsDBNull(4) ? null : reader.GetString(4),
                personBirthdate = reader.IsDBNull(5) ? (DateOnly?)null : DateOnly.FromDateTime(reader.GetDateTime(5)),
                phone = reader.IsDBNull(6) ? null : reader.GetString(6),
                mobilePhone = reader.IsDBNull(7) ? null : reader.GetString(7),
                mailingStreet = reader.IsDBNull(8) ? null : reader.GetString(8),
                mailingPostalCode = reader.IsDBNull(9) ? null : reader.GetString(9),
                mailingCity = reader.IsDBNull(10) ? null : reader.GetString(10),
                mailingCountry = reader.IsDBNull(11) ? null : reader.GetString(11),
                accountStatus = reader.IsDBNull(12) ? null : reader.GetString(12),
                accountSource = reader.IsDBNull(13) ? null : reader.GetString(13),
                hotlisted = reader.GetBoolean(14)
            };
            return new SaApiResult { StatusCode = 200, Body = JsonSerializer.Serialize(body, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
        catch (PostgresException ex)
        {
            AppLog.Error("Database error", ex);
            return new SaApiResult { StatusCode = 400, Body = JsonSerializer.Serialize(new SaErrorResponse { Error = new SaErrorBody { ErrorCode = 400, ErrorDateTime = DateTime.UtcNow, ErrorMessage = MapSqlState(ex.SqlState), ErrorDescription = ex.MessageText } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return new SaApiResult { StatusCode = 500, Body = JsonSerializer.Serialize(new SaErrorResponse { Error = new SaErrorBody { ErrorCode = 500, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "MULE UNKNOWN ERROR", ErrorDescription = "An unexpected error occurred" } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
    }

    public async Task<SaApiResult> PostAccountsAsync(string? body)
    {
        try
        {
            var doc = JsonDocument.Parse(body ?? "{}");
            var root = doc.RootElement;
            var id = Guid.NewGuid();
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync(CancellationToken.None);
            await using var cmd = new NpgsqlCommand(@"insert into public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted, created_at, updated_at) values (@id, @salutation, @first_name, @last_name, @person_email, @person_birthdate, @phone, @mobile_phone, @mailing_street, @mailing_postal_code, @mailing_city, @mailing_country, @account_status, @account_source, coalesce(@hotlisted, false), now(), now())", conn);
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("salutation", (object?)root.GetPropertyOrNull("salutation") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("first_name", (object?)root.GetPropertyOrNull("firstName") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("last_name", (object?)root.GetPropertyOrNull("lastName") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("person_email", (object?)root.GetPropertyOrNull("personEmail") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("person_birthdate", (object?)root.GetDateOnlyOrNull("personBirthdate") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("phone", (object?)root.GetPropertyOrNull("phone") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("mobile_phone", (object?)root.GetPropertyOrNull("mobilePhone") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("mailing_street", (object?)root.GetPropertyOrNull("mailingStreet") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("mailing_postal_code", (object?)root.GetPropertyOrNull("mailingPostalCode") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("mailing_city", (object?)root.GetPropertyOrNull("mailingCity") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("mailing_country", (object?)root.GetPropertyOrNull("mailingCountry") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("account_status", (object?)root.GetPropertyOrNull("accountStatus") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("account_source", (object?)root.GetPropertyOrNull("accountSource") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("hotlisted", (object?)root.GetBooleanOrNull("hotlisted") ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(CancellationToken.None);
            return new SaApiResult { StatusCode = 201, Body = JsonSerializer.Serialize(new { id }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
        catch (PostgresException ex)
        {
            AppLog.Error("Database error", ex);
            return new SaApiResult { StatusCode = 400, Body = JsonSerializer.Serialize(new SaErrorResponse { Error = new SaErrorBody { ErrorCode = 400, ErrorDateTime = DateTime.UtcNow, ErrorMessage = MapSqlState(ex.SqlState), ErrorDescription = ex.MessageText } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return new SaApiResult { StatusCode = 500, Body = JsonSerializer.Serialize(new SaErrorResponse { Error = new SaErrorBody { ErrorCode = 500, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "MULE UNKNOWN ERROR", ErrorDescription = "An unexpected error occurred" } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
    }

    public async Task<SaApiResult> PutAccountsAsync(string id, string? body)
    {
        try
        {
            var doc = JsonDocument.Parse(body ?? "{}");
            var root = doc.RootElement;
            if (!Guid.TryParse(id, out var guid)) return new SaApiResult { StatusCode = 400, Body = JsonSerializer.Serialize(new SaErrorResponse { Error = new SaErrorBody { ErrorCode = 400, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "RESOURCE NOT FOUND", ErrorDescription = "Account not found" } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync(CancellationToken.None);
            await using var check = new NpgsqlCommand("select hotlisted from public.sa_accounts where id = @id", conn);
            check.Parameters.AddWithValue("id", guid);
            var hotlistedObj = await check.ExecuteScalarAsync(CancellationToken.None);
            if (hotlistedObj is null) return new SaApiResult { StatusCode = 400, Body = JsonSerializer.Serialize(new SaErrorResponse { Error = new SaErrorBody { ErrorCode = 400, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "RESOURCE NOT FOUND", ErrorDescription = "Account not found" } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
            if (hotlistedObj is bool hotlisted && hotlisted) return new SaApiResult { StatusCode = 422, Body = JsonSerializer.Serialize(new SaErrorResponse { Error = new SaErrorBody { ErrorCode = 422, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "UNPROCESSABLE ENTITY", ErrorDescription = "Account is hotlisted and cannot be changed" } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
            await using var cmd = new NpgsqlCommand(@"update public.sa_accounts set salutation = @salutation, first_name = @first_name, last_name = @last_name, person_email = @person_email, person_birthdate = @person_birthdate, phone = @phone, mobile_phone = @mobile_phone, mailing_street = @mailing_street, mailing_postal_code = @mailing_postal_code, mailing_city = @mailing_city, mailing_country = @mailing_country, account_status = @account_status, account_source = @account_source, updated_at = now() where id = @id", conn);
            cmd.Parameters.AddWithValue("id", guid);
            cmd.Parameters.AddWithValue("salutation", (object?)root.GetPropertyOrNull("salutation") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("first_name", (object?)root.GetPropertyOrNull("firstName") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("last_name", (object?)root.GetPropertyOrNull("lastName") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("person_email", (object?)root.GetPropertyOrNull("personEmail") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("person_birthdate", (object?)root.GetDateOnlyOrNull("personBirthdate") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("phone", (object?)root.GetPropertyOrNull("phone") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("mobile_phone", (object?)root.GetPropertyOrNull("mobilePhone") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("mailing_street", (object?)root.GetPropertyOrNull("mailingStreet") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("mailing_postal_code", (object?)root.GetPropertyOrNull("mailingPostalCode") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("mailing_city", (object?)root.GetPropertyOrNull("mailingCity") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("mailing_country", (object?)root.GetPropertyOrNull("mailingCountry") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("account_status", (object?)root.GetPropertyOrNull("accountStatus") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("account_source", (object?)root.GetPropertyOrNull("accountSource") ?? DBNull.Value);
            var affected = await cmd.ExecuteNonQueryAsync(CancellationToken.None);
            if (affected == 0) return new SaApiResult { StatusCode = 400, Body = JsonSerializer.Serialize(new SaErrorResponse { Error = new SaErrorBody { ErrorCode = 400, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "RESOURCE NOT FOUND", ErrorDescription = "Account not found" } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
            return new SaApiResult { StatusCode = 200, Body = JsonSerializer.Serialize(new { id = guid }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
        catch (PostgresException ex)
        {
            AppLog.Error("Database error", ex);
            return new SaApiResult { StatusCode = 400, Body = JsonSerializer.Serialize(new SaErrorResponse { Error = new SaErrorBody { ErrorCode = 400, ErrorDateTime = DateTime.UtcNow, ErrorMessage = MapSqlState(ex.SqlState), ErrorDescription = ex.MessageText } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return new SaApiResult { StatusCode = 500, Body = JsonSerializer.Serialize(new SaErrorResponse { Error = new SaErrorBody { ErrorCode = 500, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "MULE UNKNOWN ERROR", ErrorDescription = "An unexpected error occurred" } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
    }
}

internal static class SaJsonExtensions
{
    public static string? GetPropertyOrNull(this JsonElement element, string name) => element.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null ? value.GetString() : null;
    public static DateOnly? GetDateOnlyOrNull(this JsonElement element, string name) => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && DateOnly.TryParse(value.GetString(), out var d) ? d : null;
    public static bool? GetBooleanOrNull(this JsonElement element, string name) => element.TryGetProperty(name, out var value) && (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False) ? value.GetBoolean() : null;
}
