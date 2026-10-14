using System.Data;
using System.Text.Json;
using Npgsql;

namespace BusTravelAccountsLambda;

public sealed class SaService
{
    private static readonly string ConnectionString = new SaSettings().ConnectionString;

    public async Task<SaApiResult> GetAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var email = query != null && query.TryGetValue("email", out var e) ? e?.Trim() : string.Empty;
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(@"select id, salutation, first_name, last_name, person_birthdate, phone, mobile_phone, person_email, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where lower(person_email) = lower(@email)", conn);
            cmd.Parameters.AddWithValue("email", (object?)email ?? DBNull.Value);
            await using var reader = await cmd.ExecuteReaderAsync();
            var rows = new List<Dictionary<string, object?>>();
            while (await reader.ReadAsync()) rows.Add(ReadRow(reader));
            if (rows.Count == 0) return new SaApiResult { StatusCode = 204, Body = "[]", Headers = JsonHeaders() };
            return new SaApiResult { StatusCode = 200, Body = JsonSerializer.Serialize(rows, JsonHelper.Options), Headers = JsonHeaders() };
        }
        catch (PostgresException ex)
        {
            AppLog.Error("Database error", ex);
            return Error(ex.SqlState ?? "POSTGRES", ex.MessageText ?? ex.Message);
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return Error("MULE UNKNOWN", "An unexpected error occurred");
        }
    }

    public async Task<SaApiResult> GetAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        if (!Guid.TryParse(id?.Trim(), out var guid)) return new SaApiResult { StatusCode = 204, Body = "[]", Headers = JsonHeaders() };
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(@"select id, salutation, first_name, last_name, person_birthdate, phone, mobile_phone, person_email, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where id = @id", conn);
            cmd.Parameters.AddWithValue("id", guid);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return new SaApiResult { StatusCode = 204, Body = "[]", Headers = JsonHeaders() };
            var row = ReadRow(reader);
            return new SaApiResult { StatusCode = 200, Body = JsonSerializer.Serialize(row, JsonHelper.Options), Headers = JsonHeaders() };
        }
        catch (PostgresException ex)
        {
            AppLog.Error("Database error", ex);
            return Error(ex.SqlState ?? "POSTGRES", ex.MessageText ?? ex.Message);
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return Error("MULE UNKNOWN", "An unexpected error occurred");
        }
    }

    public async Task<SaApiResult> PostAccountsAsync(string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        try
        {
            var doc = JsonDocument.Parse(body ?? "{}");
            var root = doc.RootElement;
            var id = Guid.NewGuid();
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(@"insert into public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted, created_at, updated_at) values (@id, @salutation, @first_name, @last_name, @person_email, @person_birthdate, @phone, @mobile_phone, @mailing_street, @mailing_postal_code, @mailing_city, @mailing_country, @account_status, @account_source, coalesce(@hotlisted, false), now(), now())", conn);
            Add(cmd, "id", id);
            Add(cmd, "salutation", GetString(root, "salutation"));
            Add(cmd, "first_name", GetString(root, "firstName"));
            Add(cmd, "last_name", GetString(root, "lastName"));
            Add(cmd, "person_email", GetString(root, "personEmail"));
            Add(cmd, "person_birthdate", GetDate(root, "personBirthdate"));
            Add(cmd, "phone", GetString(root, "phone"));
            Add(cmd, "mobile_phone", GetString(root, "mobilePhone"));
            Add(cmd, "mailing_street", GetString(root, "mailingStreet"));
            Add(cmd, "mailing_postal_code", GetString(root, "mailingPostalCode"));
            Add(cmd, "mailing_city", GetString(root, "mailingCity"));
            Add(cmd, "mailing_country", GetString(root, "mailingCountry"));
            Add(cmd, "account_status", GetString(root, "accountStatus"));
            Add(cmd, "account_source", GetString(root, "accountSource"));
            Add(cmd, "hotlisted", GetBool(root, "hotlisted"));
            await cmd.ExecuteNonQueryAsync();
            return new SaApiResult { StatusCode = 201, Body = JsonSerializer.Serialize(new { id = id.ToString() }, JsonHelper.Options), Headers = JsonHeaders() };
        }
        catch (PostgresException ex)
        {
            AppLog.Error("Database error", ex);
            return Error(ex.SqlState ?? "POSTGRES", ex.MessageText ?? ex.Message);
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return Error("MULE UNKNOWN", "An unexpected error occurred");
        }
    }

    public async Task<SaApiResult> PutAccountByIdAsync(string id, string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        if (!Guid.TryParse(id?.Trim(), out var guid)) return Error("404", "Account not found", 404);
        try
        {
            var doc = JsonDocument.Parse(body ?? "{}");
            var root = doc.RootElement;
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using var lookup = new NpgsqlCommand("select hotlisted from public.sa_accounts where id = @id", conn);
            lookup.Parameters.AddWithValue("id", guid);
            var hotlistedObj = await lookup.ExecuteScalarAsync();
            if (hotlistedObj == null) return Error("404", "Account not found", 404);
            if (hotlistedObj is bool hotlisted && hotlisted) return Error("422", "Account is hotlisted and cannot be changed", 422);
            await using var cmd = new NpgsqlCommand(@"update public.sa_accounts set salutation=@salutation, first_name=@first_name, last_name=@last_name, person_email=@person_email, person_birthdate=@person_birthdate, phone=@phone, mobile_phone=@mobile_phone, mailing_street=@mailing_street, mailing_postal_code=@mailing_postal_code, mailing_city=@mailing_city, mailing_country=@mailing_country, account_status=@account_status, account_source=@account_source, hotlisted=coalesce(@hotlisted, hotlisted), updated_at=now() where id=@id", conn);
            Add(cmd, "id", guid);
            Add(cmd, "salutation", GetString(root, "salutation"));
            Add(cmd, "first_name", GetString(root, "firstName"));
            Add(cmd, "last_name", GetString(root, "lastName"));
            Add(cmd, "person_email", GetString(root, "personEmail"));
            Add(cmd, "person_birthdate", GetDate(root, "personBirthdate"));
            Add(cmd, "phone", GetString(root, "phone"));
            Add(cmd, "mobile_phone", GetString(root, "mobilePhone"));
            Add(cmd, "mailing_street", GetString(root, "mailingStreet"));
            Add(cmd, "mailing_postal_code", GetString(root, "mailingPostalCode"));
            Add(cmd, "mailing_city", GetString(root, "mailingCity"));
            Add(cmd, "mailing_country", GetString(root, "mailingCountry"));
            Add(cmd, "account_status", GetString(root, "accountStatus"));
            Add(cmd, "account_source", GetString(root, "accountSource"));
            Add(cmd, "hotlisted", GetBool(root, "hotlisted"));
            var affected = await cmd.ExecuteNonQueryAsync();
            if (affected == 0) return Error("404", "Account not found", 404);
            return new SaApiResult { StatusCode = 200, Body = JsonSerializer.Serialize(new { id = guid.ToString() }, JsonHelper.Options), Headers = JsonHeaders() };
        }
        catch (PostgresException ex)
        {
            AppLog.Error("Database error", ex);
            return Error(ex.SqlState ?? "POSTGRES", ex.MessageText ?? ex.Message);
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return Error("MULE UNKNOWN", "An unexpected error occurred");
        }
    }

    public Task<SaApiResult> ReadyAsync() => Task.FromResult(new SaApiResult { StatusCode = 200, Body = "UP", Headers = JsonHeaders() });

    private static Dictionary<string, string> JsonHeaders() => new() { ["Content-Type"] = "application/json" };
    private static SaApiResult Error(string code, string description, int status = 400) => new() { StatusCode = status, Body = JsonSerializer.Serialize(new { error = new { errorCode = status, errorDateTime = DateTime.UtcNow, errorMessage = $"{code} ERROR", errorDescription = description } }, JsonHelper.Options), Headers = JsonHeaders() };
    private static void Add(NpgsqlCommand cmd, string name, object? value) => cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
    private static string? GetString(JsonElement root, string name) => root.TryGetProperty(name, out var v) && v.ValueKind != JsonValueKind.Null ? v.ValueKind == JsonValueKind.String ? v.GetString() : v.GetRawText() : null;
    private static DateOnly? GetDate(JsonElement root, string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String && DateOnly.TryParse(v.GetString(), out var d) ? d : null;
    private static bool? GetBool(JsonElement root, string name) => root.TryGetProperty(name, out var v) && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False) ? v.GetBoolean() : null;
    private static Dictionary<string, object?> ReadRow(NpgsqlDataReader reader) => new()
    {
        ["id"] = reader.GetGuid(0).ToString(),
        ["salutation"] = reader.IsDBNull(1) ? null : reader.GetString(1),
        ["firstName"] = reader.IsDBNull(2) ? null : reader.GetString(2),
        ["lastName"] = reader.IsDBNull(3) ? null : reader.GetString(3),
        ["personBirthdate"] = reader.IsDBNull(4) ? null : reader.GetDateTime(4).Date,
        ["phone"] = reader.IsDBNull(5) ? null : reader.GetString(5),
        ["mobilePhone"] = reader.IsDBNull(6) ? null : reader.GetString(6),
        ["personEmail"] = reader.IsDBNull(7) ? null : reader.GetString(7),
        ["mailingStreet"] = reader.IsDBNull(8) ? null : reader.GetString(8),
        ["mailingPostalCode"] = reader.IsDBNull(9) ? null : reader.GetString(9),
        ["mailingCity"] = reader.IsDBNull(10) ? null : reader.GetString(10),
        ["mailingCountry"] = reader.IsDBNull(11) ? null : reader.GetString(11),
        ["accountStatus"] = reader.IsDBNull(12) ? null : reader.GetString(12),
        ["accountSource"] = reader.IsDBNull(13) ? null : reader.GetString(13),
        ["hotlisted"] = reader.GetBoolean(14)
    };
}
