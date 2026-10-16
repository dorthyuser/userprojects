using System.Globalization;
using Npgsql;

namespace BusTravelAccountsLambda;

public sealed class SaAccountsService
{
    public async Task<SaApiResult> GetAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var email = (query != null && query.TryGetValue("email", out var e) ? e : null) ?? string.Empty;
        var escaped = email.Replace("'", "\\'");
        AppLog.Info($"Before Request - ListAccountsByEmail email={escaped}");
        try
        {
            await using var conn = new NpgsqlConnection(SaSettings.Instance.ConnectionString);
            await conn.OpenAsync(CancellationToken.None);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"select id, salutation, first_name, last_name, person_birthdate, phone, mobile_phone, person_email, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where lower(person_email) = lower(@email)";
            cmd.Parameters.AddWithValue("email", email);
            await using var reader = await cmd.ExecuteReaderAsync(CancellationToken.None);
            var rows = new List<object>();
            while (await reader.ReadAsync(CancellationToken.None))
            {
                rows.Add(MapRow(reader));
            }
            AppLog.Info($"After Request - ListAccountsByEmail rows={rows.Count}");
            if (rows.Count == 0)
            {
                return new SaApiResult { StatusCode = 204, Body = "[]", Headers = JsonHeaders() };
            }
            return new SaApiResult { StatusCode = 200, Body = JsonHelper.Serialize(rows), Headers = JsonHeaders() };
        }
        catch (PostgresException ex)
        {
            AppLog.Error("Database error", ex);
            return DbError(ex);
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return UnexpectedError();
        }
    }

    public async Task<SaApiResult> GetAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var accountId = string.IsNullOrWhiteSpace(id) ? null : id.Trim();
        AppLog.Info($"Before Request - GetAccountDetails id={accountId}");
        if (!Guid.TryParse(accountId, out _)) return new SaApiResult { StatusCode = 204, Body = "[]", Headers = JsonHeaders() };
        try
        {
            await using var conn = new NpgsqlConnection(SaSettings.Instance.ConnectionString);
            await conn.OpenAsync(CancellationToken.None);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"select id, salutation, first_name, last_name, person_birthdate, phone, mobile_phone, person_email, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where id = @id";
            cmd.Parameters.AddWithValue("id", Guid.Parse(accountId!));
            await using var reader = await cmd.ExecuteReaderAsync(CancellationToken.None);
            if (!await reader.ReadAsync(CancellationToken.None)) return new SaApiResult { StatusCode = 204, Body = "[]", Headers = JsonHeaders() };
            var body = JsonHelper.Serialize(MapRow(reader));
            AppLog.Info("After Request - GetAccountDetails rows=1");
            return new SaApiResult { StatusCode = 200, Body = body, Headers = JsonHeaders() };
        }
        catch (PostgresException ex)
        {
            AppLog.Error("Database error", ex);
            return DbError(ex);
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return UnexpectedError();
        }
    }

    public async Task<SaApiResult> PostAccountsAsync(string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        AppLog.Info("Before Request - Create single account");
        try
        {
            var request = JsonHelper.Deserialize<SaAccountRequest>(body) ?? throw new InvalidOperationException("Unable to parse empty input");
            var id = Guid.NewGuid();
            var insert = BuildInsert(request, id);
            await using var conn = new NpgsqlConnection(SaSettings.Instance.ConnectionString);
            await conn.OpenAsync(CancellationToken.None);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = insert.Sql;
            foreach (var p in insert.Parameters) cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(CancellationToken.None);
            AppLog.Info("After Request - Create single account rows=1");
            return new SaApiResult { StatusCode = 201, Body = JsonHelper.Serialize(new { id = id.ToString() }), Headers = JsonHeaders() };
        }
        catch (PostgresException ex)
        {
            AppLog.Error("Database error", ex);
            return DbError(ex);
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return UnexpectedError();
        }
    }

    public async Task<SaApiResult> PutAccountByIdAsync(string id, string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var accountId = string.IsNullOrWhiteSpace(id) ? null : id.Trim();
        AppLog.Info($"Before Request - Update single account id={accountId}");
        try
        {
            var request = JsonHelper.Deserialize<SaAccountRequest>(body) ?? throw new InvalidOperationException("Unable to parse empty input");
            await using var conn = new NpgsqlConnection(SaSettings.Instance.ConnectionString);
            await conn.OpenAsync(CancellationToken.None);
            await using var check = conn.CreateCommand();
            check.CommandText = "select hotlisted from public.sa_accounts where id = @id";
            check.Parameters.AddWithValue("id", Guid.Parse(accountId!));
            var existing = await check.ExecuteScalarAsync(CancellationToken.None);
            if (existing is null) return new SaApiResult { StatusCode = 400, Body = JsonHelper.Serialize(new SaErrorResponse { Error = new SaErrorBody { ErrorCode = 404, ErrorMessage = "RESOURCE NOT FOUND", ErrorDescription = "Account not found" } }), Headers = JsonHeaders() };
            if (existing is bool hotlisted && hotlisted) return new SaApiResult { StatusCode = 422, Body = JsonHelper.Serialize(new SaErrorResponse { Error = new SaErrorBody { ErrorCode = 422, ErrorMessage = "UNPROCESSABLE ENTITY", ErrorDescription = "Account is hotlisted and cannot be changed" } }), Headers = JsonHeaders() };
            var update = BuildUpdate(request, Guid.Parse(accountId!));
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = update.Sql;
            foreach (var p in update.Parameters) cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
            var rows = await cmd.ExecuteNonQueryAsync(CancellationToken.None);
            if (rows == 0) return new SaApiResult { StatusCode = 400, Body = JsonHelper.Serialize(new SaErrorResponse { Error = new SaErrorBody { ErrorCode = 404, ErrorMessage = "RESOURCE NOT FOUND", ErrorDescription = "Account not found" } }), Headers = JsonHeaders() };
            AppLog.Info("After Request - Update single account rows=1");
            return new SaApiResult { StatusCode = 200, Body = JsonHelper.Serialize(new { id = accountId }), Headers = JsonHeaders() };
        }
        catch (PostgresException ex)
        {
            AppLog.Error("Database error", ex);
            return DbError(ex);
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return UnexpectedError();
        }
    }

    private static object MapRow(NpgsqlDataReader reader) => new
    {
        id = reader.GetGuid(0).ToString(),
        salutation = Trim(reader, 1),
        firstName = Trim(reader, 2),
        lastName = Trim(reader, 3),
        personBirthdate = reader.IsDBNull(4) ? (DateOnly?)null : DateOnly.FromDateTime(reader.GetDateTime(4)),
        phone = Trim(reader, 5),
        mobilePhone = Trim(reader, 6),
        personEmail = Trim(reader, 7),
        mailingStreet = Trim(reader, 8),
        mailingPostalCode = Trim(reader, 9),
        mailingCity = Trim(reader, 10),
        mailingCountry = Trim(reader, 11),
        accountStatus = Trim(reader, 12),
        accountSource = string.IsNullOrWhiteSpace(Trim(reader, 13)) ? string.Empty : Trim(reader, 13),
        hotlisted = reader.GetBoolean(14)
    };

    private static string? Trim(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal).Trim();

    private static SaApiResult DbError(PostgresException ex) => new() { StatusCode = 400, Body = JsonHelper.Serialize(new SaErrorResponse { Error = new SaErrorBody { ErrorCode = 400, ErrorMessage = $"{ex.SqlState} ERROR", ErrorDescription = ex.MessageText } }), Headers = JsonHeaders() };
    private static SaApiResult UnexpectedError() => new() { StatusCode = 500, Body = JsonHelper.Serialize(new SaErrorResponse { Error = new SaErrorBody { ErrorCode = 500, ErrorMessage = "MULE UNKNOWN ERROR", ErrorDescription = "An unexpected error occurred" } }), Headers = JsonHeaders() };
    private static IDictionary<string, string> JsonHeaders() => new Dictionary<string, string> { ["Content-Type"] = "application/json" };

    private static (string Sql, Dictionary<string, object?> Parameters) BuildInsert(SaAccountRequest request, Guid id)
    {
        var columns = new List<string> { "id", "salutation", "first_name", "last_name", "person_email", "mailing_street", "mailing_postal_code", "mailing_city", "mailing_country", "account_status", "account_source", "hotlisted" };
        var values = new List<string> { "@id", "@salutation", "@first_name", "@last_name", "@person_email", "@mailing_street", "@mailing_postal_code", "@mailing_city", "@mailing_country", "@account_status", "@account_source", "@hotlisted" };
        var parameters = new Dictionary<string, object?>
        {
            ["id"] = id,
            ["salutation"] = Trim(request.Salutation),
            ["first_name"] = Trim(request.FirstName),
            ["last_name"] = Trim(request.LastName),
            ["person_email"] = Trim(request.PersonEmail),
            ["mailing_street"] = Trim(request.MailingStreet),
            ["mailing_postal_code"] = Trim(request.MailingPostalCode),
            ["mailing_city"] = Trim(request.MailingCity),
            ["mailing_country"] = Trim(request.MailingCountry),
            ["account_status"] = Trim(request.AccountStatus),
            ["account_source"] = Trim(request.AccountSource),
            ["hotlisted"] = request.Hotlisted ?? false
        };
        if (request.Phone is not null) { columns.Add("phone"); values.Add("@phone"); parameters["phone"] = Trim(request.Phone); }
        if (request.MobilePhone is not null) { columns.Add("mobile_phone"); values.Add("@mobile_phone"); parameters["mobile_phone"] = Trim(request.MobilePhone); }
        if (request.PersonBirthdate is not null) { columns.Add("person_birthdate"); values.Add("@person_birthdate"); parameters["person_birthdate"] = DateOnly.Parse(request.PersonBirthdate, CultureInfo.InvariantCulture); }
        return ($"insert into public.sa_accounts ({string.Join(",", columns)}) values ({string.Join(",", values)})", parameters);
    }

    private static (string Sql, Dictionary<string, object?> Parameters) BuildUpdate(SaAccountRequest request, Guid id)
    {
        var sets = new List<string>
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
            "updated_at = now()"
        };
        var parameters = new Dictionary<string, object?>
        {
            ["id"] = id,
            ["salutation"] = Trim(request.Salutation),
            ["first_name"] = Trim(request.FirstName),
            ["last_name"] = Trim(request.LastName),
            ["person_email"] = Trim(request.PersonEmail),
            ["mailing_street"] = Trim(request.MailingStreet),
            ["mailing_postal_code"] = Trim(request.MailingPostalCode),
            ["mailing_city"] = Trim(request.MailingCity),
            ["mailing_country"] = Trim(request.MailingCountry),
            ["account_status"] = Trim(request.AccountStatus),
            ["account_source"] = Trim(request.AccountSource)
        };
        if (request.Phone is not null) { sets.Add("phone = @phone"); parameters["phone"] = Trim(request.Phone); }
        if (request.MobilePhone is not null) { sets.Add("mobile_phone = @mobile_phone"); parameters["mobile_phone"] = Trim(request.MobilePhone); }
        if (request.PersonBirthdate is not null) { sets.Add("person_birthdate = @person_birthdate"); parameters["person_birthdate"] = DateOnly.Parse(request.PersonBirthdate, CultureInfo.InvariantCulture); }
        return ($"update public.sa_accounts set {string.Join(",", sets)} where id = @id", parameters);
    }

    private static string? Trim(string? value) => value?.Trim();
}
