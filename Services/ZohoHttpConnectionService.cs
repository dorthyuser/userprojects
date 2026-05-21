using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Npgsql;
using synctesting1050.Models;

namespace synctesting1050.Services;

public sealed class ZohoHttpConnectionService : IZohoHttpConnectionService
{
    private readonly IZohoHttpConnectionConnection _connection;
    private readonly ILogger<ZohoHttpConnectionService> _logger;
    private readonly NpgsqlDataSource _dataSource;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ZohoHttpConnectionService(IZohoHttpConnectionConnection connection, ILogger<ZohoHttpConnectionService> logger)
    {
        _connection = connection;
        _logger = logger;
        var host = SecretHelper.Get("POSTGRESQLHOST", "POSTGRESQLHOST");
        var port = SecretHelper.Get("POSTGRESQLPORT", "POSTGRESQLPORT");
        var database = SecretHelper.Get("POSTGRESQLDATABASE", "POSTGRESQLDATABASE");
        var username = SecretHelper.Get("POSTGRESQLUSERNAME", "POSTGRESQLUSERNAME");
        var password = SecretHelper.Get("POSTGRESQLPASSWORD", "POSTGRESQLPASSWORD");
        var connStr = $"Host={host};Port={port};Database={database};Username={username};Password={password};";
        var builder = new NpgsqlDataSourceBuilder(connStr);
        _dataSource = builder.Build();
    }

    public async Task<ApiResult> CreateUserAsync(CreateUserRequest request, string? correlationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("CreateUserAsync started. CorrelationId={CorrelationId}", correlationId ?? string.Empty);
        if (request.Users.Count != 1)
            return new ApiResult { StatusCode = 400, Body = new { status = "error", code = "VALIDATION_ERROR", message = "users array must contain exactly one item." } };

        var user = request.Users[0];
        if (string.IsNullOrWhiteSpace(user.LastName))
            return new ApiResult { StatusCode = 400, Body = new { status = "error", code = "VALIDATION_ERROR", message = "last_name is required.", field = "last_name" } };
        if (string.IsNullOrWhiteSpace(user.Email))
            return new ApiResult { StatusCode = 400, Body = new { status = "error", code = "VALIDATION_ERROR", message = "email is required.", field = "email" } };
        if (!new EmailAddressAttribute().IsValid(user.Email))
            return new ApiResult { StatusCode = 400, Body = new { status = "error", code = "INVALID_EMAIL", message = "Invalid email format." } };
        if (!string.IsNullOrWhiteSpace(user.Role) && !System.Text.RegularExpressions.Regex.IsMatch(user.Role, "^[0-9]+$"))
            return new ApiResult { StatusCode = 400, Body = new { status = "error", code = "INVALID_ROLE_ID", message = "Invalid role id." } };
        if (!string.IsNullOrWhiteSpace(user.Profile) && !System.Text.RegularExpressions.Regex.IsMatch(user.Profile, "^[0-9]+$"))
            return new ApiResult { StatusCode = 400, Body = new { status = "error", code = "INVALID_PROFILE_ID", message = "Invalid profile id." } };

        using var check = await _connection.SendAsync(HttpMethod.Get, "/crm/v8/users", "type=AllUsers&page=1&per_page=10", null, null, cancellationToken);
        var checkBody = await check.Content.ReadAsStringAsync(cancellationToken);
        if (check.IsSuccessStatusCode && checkBody.Contains(user.Email, StringComparison.OrdinalIgnoreCase))
            return new ApiResult { StatusCode = 409, Body = new { status = "error", code = "DUPLICATE_EMAIL", message = "A user with this email already exists in the Zoho org." } };

        var payload = JsonSerializer.Serialize(request, JsonOptions);
        using var response = await _connection.SendAsync(HttpMethod.Post, "/crm/v8/users", null, payload, null, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogInformation("CreateUserAsync completed. CorrelationId={CorrelationId} StatusCode={StatusCode}", correlationId ?? string.Empty, (int)response.StatusCode);
        return new ApiResult { StatusCode = (int)response.StatusCode, Body = body };
    }

    public async Task<ApiResult> GetZohoUserAsync(string zohoId, string? correlationId, string? ifModifiedSince, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetZohoUserAsync started. CorrelationId={CorrelationId} ZohoId={ZohoId}", correlationId ?? string.Empty, zohoId);
        using var response = await _connection.SendAsync(HttpMethod.Get, $"/crm/v8/users/{zohoId}", null, null, ifModifiedSince, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return new ApiResult { StatusCode = (int)response.StatusCode, Body = body };
    }

    public async Task<ApiResult> ListZohoUsersAsync(ZohoUsersListQuery query, string? correlationId, string? ifModifiedSince, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ListZohoUsersAsync started. CorrelationId={CorrelationId}", correlationId ?? string.Empty);
        var page = query.Page ?? 1;
        var perPage = query.PerPage ?? 50;
        var type = query.Type?.ToString() ?? "AllUsers";
        var qs = $"type={type}&page={page}&per_page={perPage}";
        using var response = await _connection.SendAsync(HttpMethod.Get, "/crm/v8/users", qs, null, ifModifiedSince, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return new ApiResult { StatusCode = (int)response.StatusCode, Body = body };
    }

    public async Task<ApiResult> SyncUsersAsync(SyncUsersRequest request, string? correlationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SyncUsersAsync started. CorrelationId={CorrelationId}", correlationId ?? string.Empty);
        var watermark = DateTimeOffset.UnixEpoch;
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT last_synced_at FROM sync_state WHERE sync_key = 'zoho_users'";
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        if (result is DateTimeOffset dto) watermark = dto;
        var page = 1;
        var moreRecords = true;
        var total = 0;
        while (moreRecords)
        {
            var qs = $"type={(request.Type ?? "AllUsers")}&page={page}&per_page={(request.PerPage ?? 200)}";
            using var response = await _connection.SendAsync(HttpMethod.Get, "/crm/v8/users", qs, null, watermark == DateTimeOffset.UnixEpoch ? null : watermark.ToString("o"), cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotModified) break;
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("users", out var users) && users.ValueKind == JsonValueKind.Array)
            {
                foreach (var userEl in users.EnumerateArray())
                {
                    var zohoId = userEl.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                    var modifiedText = userEl.TryGetProperty("modified_time", out var modEl) ? modEl.GetString() : null;
                    var createdText = userEl.TryGetProperty("created_time", out var crtEl) ? crtEl.GetString() : null;
                    var modified = DateTimeOffset.Parse(modifiedText ?? DateTimeOffset.UtcNow.ToString("o"));
                    await using var upsertConn = await _dataSource.OpenConnectionAsync(cancellationToken);
                    await using var upsert = upsertConn.CreateCommand();
                    upsert.CommandText = @"INSERT INTO crm_users (zoho_uid, given_name, family_name, display_name, email_address, phone_number, mobile_number, account_status, is_confirmed, user_type, zoho_role_id, zoho_role_name, zoho_profile_id, zoho_profile_name, reports_to_uid, country_code, locale_code, iana_timezone, zoho_created_at, zoho_modified_at, local_synced_at, local_created_at) VALUES (@zoho_uid, @given_name, @family_name, @display_name, @email_address, @phone_number, @mobile_number, @account_status, @is_confirmed, @user_type, @zoho_role_id, @zoho_role_name, @zoho_profile_id, @zoho_profile_name, @reports_to_uid, @country_code, @locale_code, @iana_timezone, @zoho_created_at, @zoho_modified_at, NOW(), COALESCE((SELECT local_created_at FROM crm_users WHERE zoho_uid = @zoho_uid), NOW())) ON CONFLICT (zoho_uid) DO UPDATE SET given_name = EXCLUDED.given_name, family_name = EXCLUDED.family_name, display_name = EXCLUDED.display_name, email_address = EXCLUDED.email_address, phone_number = EXCLUDED.phone_number, mobile_number = EXCLUDED.mobile_number, account_status = EXCLUDED.account_status, is_confirmed = EXCLUDED.is_confirmed, user_type = EXCLUDED.user_type, zoho_role_id = EXCLUDED.zoho_role_id, zoho_role_name = EXCLUDED.zoho_role_name, zoho_profile_id = EXCLUDED.zoho_profile_id, zoho_profile_name = EXCLUDED.zoho_profile_name, reports_to_uid = EXCLUDED.reports_to_uid, country_code = EXCLUDED.country_code, locale_code = EXCLUDED.locale_code, iana_timezone = EXCLUDED.iana_timezone, zoho_created_at = EXCLUDED.zoho_created_at, zoho_modified_at = EXCLUDED.zoho_modified_at, local_synced_at = NOW() WHERE crm_users.zoho_modified_at < EXCLUDED.zoho_modified_at;";
                    upsert.Parameters.AddWithValue("zoho_uid", (object?)zohoId ?? DBNull.Value);
                    upsert.Parameters.AddWithValue("given_name", (object?)GetJsonString(userEl, "first_name") ?? DBNull.Value);
                    upsert.Parameters.AddWithValue("family_name", (object?)GetJsonString(userEl, "last_name") ?? DBNull.Value);
                    upsert.Parameters.AddWithValue("display_name", (object?)GetJsonString(userEl, "full_name") ?? DBNull.Value);
                    upsert.Parameters.AddWithValue("email_address", (object?)GetJsonString(userEl, "email") ?? DBNull.Value);
                    upsert.Parameters.AddWithValue("phone_number", (object?)GetJsonString(userEl, "phone") ?? DBNull.Value);
                    upsert.Parameters.AddWithValue("mobile_number", (object?)GetJsonString(userEl, "mobile") ?? DBNull.Value);
                    upsert.Parameters.AddWithValue("account_status", (object?)GetJsonString(userEl, "status") ?? DBNull.Value);
                    upsert.Parameters.AddWithValue("is_confirmed", userEl.TryGetProperty("confirm", out var confEl) && confEl.ValueKind == JsonValueKind.True);
                    upsert.Parameters.AddWithValue("user_type", (object?)GetJsonString(userEl, "type__s") ?? DBNull.Value);
                    upsert.Parameters.AddWithValue("zoho_role_id", (object?)GetNestedString(userEl, "role", "id") ?? DBNull.Value);
                    upsert.Parameters.AddWithValue("zoho_role_name", (object?)GetNestedString(userEl, "role", "name") ?? DBNull.Value);
                    upsert.Parameters.AddWithValue("zoho_profile_id", (object?)GetNestedString(userEl, "profile", "id") ?? DBNull.Value);
                    upsert.Parameters.AddWithValue("zoho_profile_name", (object?)GetNestedString(userEl, "profile", "name") ?? DBNull.Value);
                    upsert.Parameters.AddWithValue("reports_to_uid", (object?)GetNestedString(userEl, "reporting_to", "id") ?? DBNull.Value);
                    upsert.Parameters.AddWithValue("country_code", (object?)GetJsonString(userEl, "country") ?? DBNull.Value);
                    upsert.Parameters.AddWithValue("locale_code", (object?)GetJsonString(userEl, "country_locale") ?? DBNull.Value);
                    upsert.Parameters.AddWithValue("iana_timezone", (object?)GetJsonString(userEl, "time_zone") ?? DBNull.Value);
                    upsert.Parameters.AddWithValue("zoho_created_at", DateTimeOffset.Parse(createdText ?? DateTimeOffset.UtcNow.ToString("o")).ToUniversalTime());
                    upsert.Parameters.AddWithValue("zoho_modified_at", modified.ToUniversalTime());
                    await upsert.ExecuteNonQueryAsync(cancellationToken);
                    total++;
                }
            }
            moreRecords = doc.RootElement.GetProperty("info").GetProperty("more_records").GetBoolean();
            page++;
        }
        await using var upd = conn.CreateCommand();
        upd.CommandText = "INSERT INTO sync_state (sync_key, last_synced_at, last_run_at, records_synced) VALUES ('zoho_users', @ts, NOW(), @cnt) ON CONFLICT (sync_key) DO UPDATE SET last_synced_at = EXCLUDED.last_synced_at, last_run_at = EXCLUDED.last_run_at, records_synced = EXCLUDED.records_synced";
        upd.Parameters.AddWithValue("ts", DateTimeOffset.UtcNow.ToUniversalTime());
        upd.Parameters.AddWithValue("cnt", total);
        await upd.ExecuteNonQueryAsync(cancellationToken);
        return new ApiResult { StatusCode = 200, Body = new { status = "success", watermark_used = watermark, new_watermark = DateTimeOffset.UtcNow, pages_fetched = page - 1, zoho_records_read = total, upserted = total, unchanged = 0, errors = 0, sync_duration_ms = 0 } };
    }

    public async Task<ApiResult> GetLocalUsersAsync(LocalUsersQuery query, string? correlationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetLocalUsersAsync started. CorrelationId={CorrelationId}", correlationId ?? string.Empty);
        var page = query.Page ?? 1;
        var pageSize = query.PageSize ?? 50;
        var offset = (page - 1) * pageSize;
        var where = new List<string>();
        var parameters = new List<NpgsqlParameter>();
        if (!string.IsNullOrWhiteSpace(query.AccountStatus)) { where.Add("account_status = @account_status"); parameters.Add(new NpgsqlParameter("account_status", query.AccountStatus)); }
        if (!string.IsNullOrWhiteSpace(query.ZohoRoleId)) { where.Add("zoho_role_id = @zoho_role_id"); parameters.Add(new NpgsqlParameter("zoho_role_id", query.ZohoRoleId)); }
        if (!string.IsNullOrWhiteSpace(query.ZohoProfileId)) { where.Add("zoho_profile_id = @zoho_profile_id"); parameters.Add(new NpgsqlParameter("zoho_profile_id", query.ZohoProfileId)); }
        if (query.IsConfirmed.HasValue) { where.Add("is_confirmed = @is_confirmed"); parameters.Add(new NpgsqlParameter("is_confirmed", query.IsConfirmed.Value)); }
        if (query.SyncedAfter.HasValue) { where.Add("local_synced_at >= @synced_after"); parameters.Add(new NpgsqlParameter("synced_after", query.SyncedAfter.Value.ToUniversalTime())); }
        var sql = "SELECT * FROM crm_users" + (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "") + $" ORDER BY {(query.SortBy ?? "family_name")} {(query.SortOrder ?? "asc")} LIMIT @limit OFFSET @offset";
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var p in parameters) cmd.Parameters.Add(p);
        cmd.Parameters.AddWithValue("limit", pageSize);
        cmd.Parameters.AddWithValue("offset", offset);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var users = new List<LocalUserDto>();
        while (await reader.ReadAsync(cancellationToken)) users.Add(MapLocalUser(reader));
        return new ApiResult { StatusCode = users.Count == 0 ? 404 : 200, Body = users.Count == 0 ? new { status = "error", code = "USER_NOT_FOUND", message = "No local user record found for the given identifier." } : new { status = "success", page, page_size = pageSize, total_count = users.Count, users } };
    }

    public async Task<ApiResult> GetLocalUserByPkAsync(long userPk, string? correlationId, CancellationToken cancellationToken = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM crm_users WHERE user_pk = @user_pk";
        cmd.Parameters.AddWithValue("user_pk", userPk);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return new ApiResult { StatusCode = 404, Body = new { status = "error", code = "USER_NOT_FOUND", message = "No local user record found for the given identifier." } };
        return new ApiResult { StatusCode = 200, Body = new { status = "success", user = MapLocalUser(reader) } };
    }

    public async Task<ApiResult> GetLocalUserByZohoUidAsync(string zohoUid, string? correlationId, CancellationToken cancellationToken = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM crm_users WHERE zoho_uid = @zoho_uid";
        cmd.Parameters.AddWithValue("zoho_uid", zohoUid);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return new ApiResult { StatusCode = 404, Body = new { status = "error", code = "USER_NOT_FOUND", message = "No local user record found for the given identifier." } };
        return new ApiResult { StatusCode = 200, Body = new { status = "success", user = MapLocalUser(reader) } };
    }

    private static string? GetJsonString(JsonElement element, string propertyName) => element.TryGetProperty(propertyName, out var prop) ? prop.GetString() : null;

    private static string? GetNestedString(JsonElement element, string propertyName, string nestedName)
    {
        if (!element.TryGetProperty(propertyName, out var nested) || nested.ValueKind != JsonValueKind.Object) return null;
        return nested.TryGetProperty(nestedName, out var child) ? child.GetString() : null;
    }

    private static LocalUserDto MapLocalUser(NpgsqlDataReader reader) => new()
    {
        UserPk = reader.GetInt64(reader.GetOrdinal("user_pk")),
        ZohoUid = reader["zoho_uid"] as string,
        GivenName = reader["given_name"] as string,
        FamilyName = reader["family_name"] as string,
        DisplayName = reader["display_name"] as string,
        EmailAddress = reader["email_address"] as string,
        PhoneNumber = reader["phone_number"] as string,
        MobileNumber = reader["mobile_number"] as string,
        AccountStatus = reader["account_status"] as string,
        IsConfirmed = reader["is_confirmed"] as bool?,
        UserType = reader["user_type"] as string,
        ZohoRoleId = reader["zoho_role_id"] as string,
        ZohoRoleName = reader["zoho_role_name"] as string,
        ZohoProfileId = reader["zoho_profile_id"] as string,
        ZohoProfileName = reader["zoho_profile_name"] as string,
        ReportsToUid = reader["reports_to_uid"] as string,
        CountryCode = reader["country_code"] as string,
        LocaleCode = reader["locale_code"] as string,
        IanaTimezone = reader["iana_timezone"] as string,
        ZohoCreatedAt = reader["zoho_created_at"] as DateTimeOffset?,
        ZohoModifiedAt = reader["zoho_modified_at"] as DateTimeOffset?,
        LocalSyncedAt = reader["local_synced_at"] as DateTimeOffset?
    };
}