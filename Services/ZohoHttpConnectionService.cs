using System.Text.Json;
using System.Text.RegularExpressions;
using Npgsql;
using synctesting1109.Models;

namespace synctesting1109.Services;

public sealed class ZohoHttpConnectionService : IZohoHttpConnectionService
{
    private static readonly Regex ZohoIdRegex    = new(@"^[0-9]+$", RegexOptions.Compiled);
    private static readonly string[] ValidTypes  = ["AllUsers", "ActiveUsers", "DeactiveUsers", "ConfirmedUsers", "AdminUsers"];
    private static readonly string[] ValidLocalSorts = ["family_name", "email_address", "zoho_modified_at", "local_synced_at"];

    private readonly IZohoHttpConnectionConnection _connection;
    private readonly ILogger<ZohoHttpConnectionService> _logger;
    private readonly NpgsqlDataSource _dataSource;

    public ZohoHttpConnectionService(IZohoHttpConnectionConnection connection, ILogger<ZohoHttpConnectionService> logger)
    {
        _connection = connection;
        _logger = logger;
        var host     = SecretHelper.Get("POSTGRESQLHOST",     "POSTGRESQLHOST");
        var port     = SecretHelper.Get("POSTGRESQLPORT",     "POSTGRESQLPORT");
        var database = SecretHelper.Get("POSTGRESQLDATABASE", "POSTGRESQLDATABASE");
        var username = SecretHelper.Get("POSTGRESQLUSERNAME", "POSTGRESQLUSERNAME");
        var password = SecretHelper.Get("POSTGRESQLPASSWORD", "POSTGRESQLPASSWORD");
        var connStr  = $"Host={host};Port={port};Database={database};Username={username};Password={password};";
        _dataSource  = new NpgsqlDataSourceBuilder(connStr).Build();
    }

    // ── Endpoint 1 — Create user in Zoho CRM ────────────────────────────────

    public async Task<ApiResult> CreateUserAsync(ZohoCreateUserRequest model, string? authorization, string? correlationId, CancellationToken cancellationToken = default)
    {
        ValidateAuth(authorization);
        ValidateCreateRequest(model);

        // Check for duplicate email before creating
        using var checkResponse = await _connection.SendAsync(
            HttpMethod.Get, "/crm/v8/users?type=AllUsers&page=1&per_page=200", null, correlationId, cancellationToken);
        var checkBody = await checkResponse.Content.ReadAsStringAsync(cancellationToken);
        var item = model.Users[0];
        if (checkBody.Contains(item.Email ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            return new ApiResult
            {
                StatusCode    = 409,
                Body          = new { status = "error", code = "DUPLICATE_EMAIL", message = "A user with this email already exists in the Zoho org." },
                CorrelationId = correlationId
            };

        var body = JsonSerializer.Serialize(model);
        using var response = await _connection.SendAsync(HttpMethod.Post, "/crm/v8/users", body, correlationId, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Created)
        {
            using var doc = JsonDocument.Parse(responseBody);
            var id = doc.RootElement
                        .TryGetProperty("users", out var users) &&
                     users.GetArrayLength() > 0 &&
                     users[0].TryGetProperty("details", out var details) &&
                     details.TryGetProperty("id", out var idEl)
                        ? idEl.GetString() : null;

            return new ApiResult
            {
                StatusCode    = 201,
                Body          = new { status = "success", zoho_id = id, email = item.Email, created_at = DateTimeOffset.UtcNow.ToString("O") },
                CorrelationId = correlationId
            };
        }

        return new ApiResult { StatusCode = (int)response.StatusCode, Body = responseBody, CorrelationId = correlationId };
    }

    // ── Endpoint 2 — Get users from Zoho CRM ────────────────────────────────

    public async Task<ApiResult> GetZohoUsersAsync(ZohoGetUsersQuery query, string? authorization, string? correlationId, CancellationToken cancellationToken = default)
    {
        ValidateAuth(authorization);
        var type    = string.IsNullOrWhiteSpace(query.Type) ? "AllUsers" : query.Type;
        var page    = query.Page ?? 1;
        var perPage = query.PerPage ?? 50;
        if (!ValidTypes.Contains(type))      throw new ArgumentException("INVALID_TYPE");
        if (page < 1)                        throw new ArgumentException("INVALID_PAGE");
        if (perPage is < 1 or > 200)         throw new ArgumentException("INVALID_PER_PAGE");
        if (!string.IsNullOrWhiteSpace(query.IfModifiedSince) && !DateTimeOffset.TryParse(query.IfModifiedSince, out _))
            throw new ArgumentException("INVALID_DATE");

        var relative = $"/crm/v8/users?type={Uri.EscapeDataString(type)}&page={page}&per_page={perPage}";
        using var response = await _connection.SendAsync(HttpMethod.Get, relative, null, correlationId, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return new ApiResult { StatusCode = (int)response.StatusCode, Body = responseBody, CorrelationId = correlationId };
    }

    public async Task<ApiResult> GetZohoUserByIdAsync(string zohoId, ZohoGetUsersQuery query, string? authorization, string? correlationId, CancellationToken cancellationToken = default)
    {
        ValidateAuth(authorization);
        if (!ZohoIdRegex.IsMatch(zohoId)) throw new ArgumentException("INVALID_ZOHO_ID");
        using var response = await _connection.SendAsync(HttpMethod.Get, $"/crm/v8/users/{zohoId}", null, correlationId, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return new ApiResult { StatusCode = (int)response.StatusCode, Body = responseBody, CorrelationId = correlationId };
    }

    // ── Endpoint 3 — Delta sync Zoho → PostgreSQL ───────────────────────────

    public async Task<ApiResult> SyncUsersAsync(ZohoSyncUsersRequest model, string? authorization, string? correlationId, CancellationToken cancellationToken = default)
    {
        ValidateAuth(authorization);
        if (model.PerPage.HasValue && model.PerPage is < 1 or > 200) throw new ArgumentException("INVALID_PER_PAGE");
        if (!string.IsNullOrWhiteSpace(model.Type) &&
            !new[] { "AllUsers", "ActiveUsers", "DeactiveUsers" }.Contains(model.Type))
            throw new ArgumentException("INVALID_TYPE");

        var syncType  = string.IsNullOrWhiteSpace(model.Type) ? "AllUsers" : model.Type;
        var perPage = model.PerPage ?? 200;
        var syncStart = DateTimeOffset.UtcNow;

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);

        // Read watermark from sync_state
        var watermark = DateTimeOffset.UnixEpoch;
        if (model.FullSync != true)
        {
            await using var wCmd = conn.CreateCommand();
            wCmd.CommandText = "SELECT last_synced_at FROM sync_state WHERE sync_key = 'zoho_users'";
            var scalar = await wCmd.ExecuteScalarAsync(cancellationToken);
            if (scalar is DateTimeOffset dto) watermark = dto;
        }

        var page         = 1;
        var moreRecords  = true;
        var newWatermark = watermark;
        var upserted     = 0;
        var unchanged    = 0;
        var errors       = 0;
        var pagesFetched = 0;
        var totalRead    = 0;
        var errorDetails = new List<object>();

        while (moreRecords)
        {
            var path = $"/crm/v8/users?type={Uri.EscapeDataString(syncType)}&page={page}&per_page={perPage}";
            using var response = await _connection.SendAsync(HttpMethod.Get, path, null, correlationId, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
                break;

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(body);

            if (!doc.RootElement.TryGetProperty("users", out var usersEl))
                break;

            pagesFetched++;
            var users = usersEl.EnumerateArray().ToList();
            totalRead += users.Count;

            foreach (var user in users)
            {
                var zohoUid = "";
                try
                {
                    zohoUid = user.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
                    var modifiedTime = user.TryGetProperty("modified_time", out var mt) &&
                                       DateTimeOffset.TryParse(mt.GetString(), out var parsedMt)
                        ? parsedMt : DateTimeOffset.UtcNow;

                    if (modifiedTime > newWatermark)
                        newWatermark = modifiedTime;

                    await using var uCmd = conn.CreateCommand();
                    uCmd.CommandText = @"
                        INSERT INTO crm_users (
                            zoho_uid, given_name, family_name, display_name, email_address,
                            phone_number, mobile_number, account_status, is_confirmed, user_type,
                            zoho_role_id, zoho_role_name, zoho_profile_id, zoho_profile_name,
                            reports_to_uid, country_code, locale_code, iana_timezone,
                            zoho_created_at, zoho_modified_at, local_synced_at
                        ) VALUES (
                            @zoho_uid, @given_name, @family_name, @display_name, @email_address,
                            @phone_number, @mobile_number, @account_status, @is_confirmed, @user_type,
                            @zoho_role_id, @zoho_role_name, @zoho_profile_id, @zoho_profile_name,
                            @reports_to_uid, @country_code, @locale_code, @iana_timezone,
                            @zoho_created_at, @zoho_modified_at, NOW()
                        )
                        ON CONFLICT (zoho_uid) DO UPDATE SET
                            given_name        = EXCLUDED.given_name,
                            family_name       = EXCLUDED.family_name,
                            display_name      = EXCLUDED.display_name,
                            email_address     = EXCLUDED.email_address,
                            phone_number      = EXCLUDED.phone_number,
                            mobile_number     = EXCLUDED.mobile_number,
                            account_status    = EXCLUDED.account_status,
                            is_confirmed      = EXCLUDED.is_confirmed,
                            user_type         = EXCLUDED.user_type,
                            zoho_role_id      = EXCLUDED.zoho_role_id,
                            zoho_role_name    = EXCLUDED.zoho_role_name,
                            zoho_profile_id   = EXCLUDED.zoho_profile_id,
                            zoho_profile_name = EXCLUDED.zoho_profile_name,
                            reports_to_uid    = EXCLUDED.reports_to_uid,
                            country_code      = EXCLUDED.country_code,
                            locale_code       = EXCLUDED.locale_code,
                            iana_timezone     = EXCLUDED.iana_timezone,
                            zoho_created_at   = EXCLUDED.zoho_created_at,
                            zoho_modified_at  = EXCLUDED.zoho_modified_at,
                            local_synced_at   = NOW()
                        WHERE EXCLUDED.zoho_modified_at > crm_users.zoho_modified_at";

                    uCmd.Parameters.AddWithValue("zoho_uid",         zohoUid);
                    uCmd.Parameters.AddWithValue("given_name",        GetStr(user, "first_name"));
                    uCmd.Parameters.AddWithValue("family_name",       GetStr(user, "last_name"));
                    uCmd.Parameters.AddWithValue("display_name",      GetStr(user, "full_name"));
                    uCmd.Parameters.AddWithValue("email_address",     GetStr(user, "email"));
                    uCmd.Parameters.AddWithValue("phone_number",      GetStr(user, "phone"));
                    uCmd.Parameters.AddWithValue("mobile_number",     GetStr(user, "mobile"));
                    uCmd.Parameters.AddWithValue("account_status",    GetStr(user, "status"));
                    uCmd.Parameters.AddWithValue("is_confirmed",      GetBool(user, "confirm"));
                    uCmd.Parameters.AddWithValue("user_type",         GetStr(user, "type__s"));
                    uCmd.Parameters.AddWithValue("zoho_role_id",      GetNested(user, "role", "id"));
                    uCmd.Parameters.AddWithValue("zoho_role_name",    GetNested(user, "role", "name"));
                    uCmd.Parameters.AddWithValue("zoho_profile_id",   GetNested(user, "profile", "id"));
                    uCmd.Parameters.AddWithValue("zoho_profile_name", GetNested(user, "profile", "name"));
                    uCmd.Parameters.AddWithValue("reports_to_uid",    GetNested(user, "reporting_to", "id"));
                    uCmd.Parameters.AddWithValue("country_code",      GetStr(user, "country"));
                    uCmd.Parameters.AddWithValue("locale_code",       GetStr(user, "country_locale"));
                    uCmd.Parameters.AddWithValue("iana_timezone",     GetStr(user, "time_zone"));
                    uCmd.Parameters.AddWithValue("zoho_created_at",   ParseDate(user, "created_time"));
                    uCmd.Parameters.AddWithValue("zoho_modified_at",  modifiedTime);

                    var rows = await uCmd.ExecuteNonQueryAsync(cancellationToken);
                    if (rows > 0) upserted++; else unchanged++;
                }
                catch (Exception ex)
                {
                    errors++;
                    errorDetails.Add(new { zoho_id = zohoUid, reason = ex.Message });
                    _logger.LogError(ex, "Upsert failed for zoho_id={ZohoId}", zohoUid);
                }
            }

            moreRecords = doc.RootElement.TryGetProperty("info", out var info) &&
                          info.TryGetProperty("more_records", out var mr) &&
                          mr.GetBoolean();
            page++;
        }

        // Update watermark only on full or partial success
        if (pagesFetched > 0 && errors < totalRead)
        {
            await using var wUpd = conn.CreateCommand();
            wUpd.CommandText = @"
                INSERT INTO sync_state (sync_key, last_synced_at, last_run_at, records_synced)
                VALUES ('zoho_users', @w, NOW(), @count)
                ON CONFLICT (sync_key) DO UPDATE SET
                    last_synced_at = @w, last_run_at = NOW(), records_synced = @count";
            wUpd.Parameters.AddWithValue("w",     newWatermark);
            wUpd.Parameters.AddWithValue("count", upserted);
            await wUpd.ExecuteNonQueryAsync(cancellationToken);
        }

        var durationMs = (long)(DateTimeOffset.UtcNow - syncStart).TotalMilliseconds;

        if (errors > 0 && upserted > 0)
            return new ApiResult
            {
                StatusCode    = 207,
                Body          = new { status = "partial", upserted, errors, error_detail = errorDetails },
                CorrelationId = correlationId
            };

        return new ApiResult
        {
            StatusCode    = 200,
            Body          = new
            {
                status             = "success",
                watermark_used     = watermark.ToString("O"),
                new_watermark      = newWatermark.ToString("O"),
                pages_fetched      = pagesFetched,
                zoho_records_read  = totalRead,
                upserted,
                unchanged,
                errors,
                sync_duration_ms   = durationMs
            },
            CorrelationId = correlationId
        };
    }

    // ── Endpoint 4 — Get users from PostgreSQL ───────────────────────────────

    public async Task<ApiResult> GetLocalUsersAsync(LocalUsersQuery query, string? authorization, string? correlationId, CancellationToken cancellationToken = default)
    {
        ValidateAuth(authorization);
        if (!string.IsNullOrWhiteSpace(query.AccountStatus) && query.AccountStatus is not ("active" or "inactive"))
            throw new ArgumentException("INVALID_STATUS");
        if (!string.IsNullOrWhiteSpace(query.ZohoRoleId)    && !ZohoIdRegex.IsMatch(query.ZohoRoleId))
            throw new ArgumentException("INVALID_SORT");
        if (!string.IsNullOrWhiteSpace(query.ZohoProfileId) && !ZohoIdRegex.IsMatch(query.ZohoProfileId))
            throw new ArgumentException("INVALID_SORT");
        if (!string.IsNullOrWhiteSpace(query.SyncedAfter)   && !DateTimeOffset.TryParse(query.SyncedAfter, out _))
            throw new ArgumentException("INVALID_DATE");

        var page      = query.Page ?? 1;
        var pageSize  = query.PageSize ?? 50;
        if (page < 1)               throw new ArgumentException("INVALID_PAGE");
        if (pageSize is < 1 or > 500) throw new ArgumentException("INVALID_PAGE_SIZE");

        var sortBy    = ValidLocalSorts.Contains(query.SortBy ?? "") ? query.SortBy : "family_name";
        var sortOrder = query.SortOrder == "desc" ? "DESC" : "ASC";
        var offset    = (page - 1) * pageSize;

        // Build WHERE clause
        var conditions = new List<string>();
        var parameters = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(query.AccountStatus))
            { conditions.Add("account_status = @status");     parameters["status"]      = query.AccountStatus; }
        if (!string.IsNullOrWhiteSpace(query.ZohoRoleId))
            { conditions.Add("zoho_role_id = @role_id");      parameters["role_id"]     = query.ZohoRoleId; }
        if (!string.IsNullOrWhiteSpace(query.ZohoProfileId))
            { conditions.Add("zoho_profile_id = @profile_id"); parameters["profile_id"] = query.ZohoProfileId; }
        if (query.IsConfirmed.HasValue)
            { conditions.Add("is_confirmed = @confirmed");    parameters["confirmed"]   = query.IsConfirmed.Value; }
        if (!string.IsNullOrWhiteSpace(query.SyncedAfter) && DateTimeOffset.TryParse(query.SyncedAfter, out var sa))
            { conditions.Add("local_synced_at >= @synced_after"); parameters["synced_after"] = sa; }

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);

        // Count query
        await using var countCmd = conn.CreateCommand();
        countCmd.CommandText = $"SELECT COUNT(*) FROM crm_users {whereClause}";
        foreach (var p in parameters) countCmd.Parameters.AddWithValue(p.Key, p.Value);
        var totalCount = (long)(await countCmd.ExecuteScalarAsync(cancellationToken) ?? 0L);

        // Data query
        await using var dataCmd = conn.CreateCommand();
        dataCmd.CommandText = $@"
            SELECT * FROM crm_users {whereClause}
            ORDER BY {sortBy} {sortOrder}
            LIMIT @limit OFFSET @offset";
        foreach (var p in parameters) dataCmd.Parameters.AddWithValue(p.Key, p.Value);
        dataCmd.Parameters.AddWithValue("limit",  pageSize);
        dataCmd.Parameters.AddWithValue("offset", offset);

        var users = new List<LocalUserDto>();
        await using var reader = await dataCmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            users.Add(MapLocalUser(reader));

        return new ApiResult
        {
            StatusCode    = 200,
            Body          = new { status = "success", page, page_size = pageSize, total_count = totalCount, users },
            CorrelationId = correlationId
        };
    }

    public async Task<ApiResult> GetLocalUserByPkAsync(long userPk, string? authorization, string? correlationId, CancellationToken cancellationToken = default)
    {
        ValidateAuth(authorization);
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd  = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM crm_users WHERE user_pk = @pk LIMIT 1";
        cmd.Parameters.AddWithValue("pk", userPk);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return new ApiResult { StatusCode = 404, Body = new { status = "error", code = "USER_NOT_FOUND", message = "No local user record found for the given identifier." }, CorrelationId = correlationId };
        return new ApiResult { StatusCode = 200, Body = new { status = "success", user = MapLocalUser(reader) }, CorrelationId = correlationId };
    }

    public async Task<ApiResult> GetLocalUserByZohoUidAsync(string zohoUid, string? authorization, string? correlationId, CancellationToken cancellationToken = default)
    {
        ValidateAuth(authorization);
        if (!ZohoIdRegex.IsMatch(zohoUid)) throw new ArgumentException("INVALID_ZOHO_UID");
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd  = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM crm_users WHERE zoho_uid = @uid LIMIT 1";
        cmd.Parameters.AddWithValue("uid", zohoUid);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return new ApiResult { StatusCode = 404, Body = new { status = "error", code = "USER_NOT_FOUND", message = "No local user record found for the given identifier." }, CorrelationId = correlationId };
        return new ApiResult { StatusCode = 200, Body = new { status = "success", user = MapLocalUser(reader) }, CorrelationId = correlationId };
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static void ValidateAuth(string? authorization)
    {
        if (string.IsNullOrWhiteSpace(authorization) ||
            !authorization.StartsWith("Zoho-oauthtoken ", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("UNAUTHORIZED");
    }

    private static void ValidateCreateRequest(ZohoCreateUserRequest request)
    {
        if (request.Users.Count != 1)                          throw new ArgumentException("VALIDATION_ERROR");
        var item = request.Users[0];
        if (string.IsNullOrWhiteSpace(item.LastName))          throw new ArgumentException("last_name is required.");
        if (string.IsNullOrWhiteSpace(item.Email))             throw new ArgumentException("email is required.");
        if (!ZohoIdRegex.IsMatch(item.Role    ?? string.Empty)) throw new ArgumentException("INVALID_ROLE_ID");
        if (!ZohoIdRegex.IsMatch(item.Profile ?? string.Empty)) throw new ArgumentException("INVALID_PROFILE_ID");
    }

    private static LocalUserDto MapLocalUser(NpgsqlDataReader r)
    {
        T? Safe<T>(string col, Func<int, T> getter) where T : struct
        {
            var i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? null : getter(i);
        }
        string? SafeStr(string col)
        {
            var i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? null : r.GetString(i);
        }

        return new LocalUserDto
        {
            UserPk          = r.GetInt64(r.GetOrdinal("user_pk")),
            ZohoUid         = SafeStr("zoho_uid"),
            GivenName       = SafeStr("given_name"),
            FamilyName      = SafeStr("family_name"),
            DisplayName     = SafeStr("display_name"),
            EmailAddress    = SafeStr("email_address"),
            PhoneNumber     = SafeStr("phone_number"),
            MobileNumber    = SafeStr("mobile_number"),
            AccountStatus   = SafeStr("account_status"),
            IsConfirmed     = Safe("is_confirmed",   r.GetBoolean),
            UserType        = SafeStr("user_type"),
            ZohoRoleId      = SafeStr("zoho_role_id"),
            ZohoRoleName    = SafeStr("zoho_role_name"),
            ZohoProfileId   = SafeStr("zoho_profile_id"),
            ZohoProfileName = SafeStr("zoho_profile_name"),
            ReportsToUid    = SafeStr("reports_to_uid"),
            CountryCode     = SafeStr("country_code"),
            LocaleCode      = SafeStr("locale_code"),
            IanaTimezone    = SafeStr("iana_timezone"),
            ZohoCreatedAt   = Safe("zoho_created_at",  r.GetFieldValue<DateTimeOffset>),
            ZohoModifiedAt  = Safe("zoho_modified_at", r.GetFieldValue<DateTimeOffset>),
            LocalSyncedAt   = Safe("local_synced_at",  r.GetFieldValue<DateTimeOffset>)
        };
    }

    private static string GetStr(JsonElement el, string key) =>
        el.TryGetProperty(key, out var v) ? v.GetString() ?? "" : "";

    private static bool GetBool(JsonElement el, string key) =>
        el.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.True;

    private static string GetNested(JsonElement el, string parent, string child)
    {
        if (!el.TryGetProperty(parent, out var p)) return "";
        if (!p.TryGetProperty(child,   out var c)) return "";
        return c.GetString() ?? "";
    }

    private static DateTimeOffset ParseDate(JsonElement el, string key) =>
        el.TryGetProperty(key, out var v) &&
        DateTimeOffset.TryParse(v.GetString(), out var d) ? d : DateTimeOffset.UtcNow;
}
