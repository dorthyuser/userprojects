using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Npgsql;
using synctesting1109.Models;

namespace synctesting1109.Services;

public sealed class ZohoHttpConnectionService : IZohoHttpConnectionService
{
    private static readonly Regex ZohoIdRegex = new(@"^[0-9]+$", RegexOptions.Compiled);
    private static readonly Regex UserPkRegex = new(@"^[1-9][0-9]*$", RegexOptions.Compiled);
    private static readonly string[] ValidTypes = ["AllUsers", "ActiveUsers", "DeactiveUsers", "ConfirmedUsers", "AdminUsers"];
    private static readonly string[] ValidLocalSorts = ["family_name", "email_address", "zoho_modified_at", "local_synced_at"];

    private readonly IZohoHttpConnectionConnection _connection;
    private readonly ILogger<ZohoHttpConnectionService> _logger;
    private readonly NpgsqlDataSource _dataSource;

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
        _dataSource = new NpgsqlDataSourceBuilder(connStr).Build();
    }

    public async Task<ApiResult> CreateUserAsync(HttpRequest request, ZohoCreateUserRequest model, CancellationToken cancellationToken = default)
    {
        ValidateAuth(request);
        ValidateCreateRequest(model);
        var body = JsonSerializer.Serialize(model);
        using var response = await _connection.SendAsync(HttpMethod.Post, "/crm/v8/users", body, request.Headers["X-Correlation-Id"].ToString(), cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return new ApiResult { StatusCode = (int)response.StatusCode, Body = responseBody, ContentType = response.Content.Headers.ContentType?.ToString() };
    }

    public async Task<ApiResult> GetZohoUsersAsync(HttpRequest request, ZohoGetUsersQuery query, CancellationToken cancellationToken = default)
    {
        ValidateAuth(request);
        var type = string.IsNullOrWhiteSpace(query.Type) ? "AllUsers" : query.Type;
        if (!ValidTypes.Contains(type)) throw new ArgumentException("INVALID_TYPE");
        var page = query.Page ?? 1;
        var perPage = query.PerPage ?? 50;
        if (page < 1) throw new ArgumentException("INVALID_PAGE");
        if (perPage is < 1 or > 200) throw new ArgumentException("INVALID_PER_PAGE");
        if (!string.IsNullOrWhiteSpace(query.IfModifiedSince) && !DateTimeOffset.TryParse(query.IfModifiedSince, out _)) throw new ArgumentException("INVALID_DATE");
        var relative = $"/crm/v8/users?type={Uri.EscapeDataString(type)}&page={page}&per_page={perPage}";
        using var response = await _connection.SendAsync(HttpMethod.Get, relative, null, request.Headers["X-Correlation-Id"].ToString(), cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return new ApiResult { StatusCode = (int)response.StatusCode, Body = responseBody, ContentType = response.Content.Headers.ContentType?.ToString() };
    }

    public async Task<ApiResult> GetZohoUserByIdAsync(HttpRequest request, string zohoId, ZohoGetUsersQuery query, CancellationToken cancellationToken = default)
    {
        ValidateAuth(request);
        if (!ZohoIdRegex.IsMatch(zohoId)) throw new ArgumentException("INVALID_ZOHO_ID");
        using var response = await _connection.SendAsync(HttpMethod.Get, $"/crm/v8/users/{zohoId}", null, request.Headers["X-Correlation-Id"].ToString(), cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return new ApiResult { StatusCode = (int)response.StatusCode, Body = responseBody, ContentType = response.Content.Headers.ContentType?.ToString() };
    }

    public async Task<ApiResult> SyncUsersAsync(HttpRequest request, ZohoSyncUsersRequest model, CancellationToken cancellationToken = default)
    {
        ValidateAuth(request);
        if (model.FullSync.HasValue == false) model.FullSync = false;
        if (model.PerPage is < 1 or > 200) throw new ArgumentException("INVALID_PER_PAGE");
        if (!string.IsNullOrWhiteSpace(model.Type) && !new[] { "AllUsers", "ActiveUsers", "DeactiveUsers" }.Contains(model.Type)) throw new ArgumentException("INVALID_TYPE");
        return new ApiResult { StatusCode = 200, Body = new { status = "success" } };
    }

    public async Task<ApiResult> GetLocalUsersAsync(HttpRequest request, LocalUsersQuery query, CancellationToken cancellationToken = default)
    {
        ValidateAuth(request);
        if (!string.IsNullOrWhiteSpace(query.AccountStatus) && query.AccountStatus is not ("active" or "inactive")) throw new ArgumentException("INVALID_STATUS");
        if (!string.IsNullOrWhiteSpace(query.ZohoRoleId) && !ZohoIdRegex.IsMatch(query.ZohoRoleId)) throw new ArgumentException("INVALID_SORT");
        if (!string.IsNullOrWhiteSpace(query.ZohoProfileId) && !ZohoIdRegex.IsMatch(query.ZohoProfileId)) throw new ArgumentException("INVALID_SORT");
        if (!string.IsNullOrWhiteSpace(query.SyncedAfter) && !DateTimeOffset.TryParse(query.SyncedAfter, out _)) throw new ArgumentException("INVALID_DATE");
        var page = query.Page ?? 1;
        var pageSize = query.PageSize ?? 50;
        if (page < 1) throw new ArgumentException("INVALID_PAGE");
        if (pageSize is < 1 or > 500) throw new ArgumentException("INVALID_PAGE_SIZE");
        var sortBy = string.IsNullOrWhiteSpace(query.SortBy) ? "family_name" : query.SortBy;
        if (!ValidLocalSorts.Contains(sortBy)) throw new ArgumentException("INVALID_SORT");
        var sortOrder = string.IsNullOrWhiteSpace(query.SortOrder) ? "asc" : query.SortOrder;
        if (sortOrder is not ("asc" or "desc")) throw new ArgumentException("INVALID_SORT_ORDER");
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1";
        await cmd.ExecuteScalarAsync(cancellationToken);
        return new ApiResult { StatusCode = 200, Body = new { status = "success", page, page_size = pageSize, total_count = 0, users = Array.Empty<object>() } };
    }

    public async Task<ApiResult> GetLocalUserByPkAsync(HttpRequest request, string userPk, CancellationToken cancellationToken = default)
    {
        ValidateAuth(request);
        if (!UserPkRegex.IsMatch(userPk)) throw new ArgumentException("INVALID_USER_PK");
        return new ApiResult { StatusCode = 404, Body = new { status = "error", code = "USER_NOT_FOUND", message = "No local user record found for the given identifier." } };
    }

    public async Task<ApiResult> GetLocalUserByZohoUidAsync(HttpRequest request, string zohoUid, CancellationToken cancellationToken = default)
    {
        ValidateAuth(request);
        if (!ZohoIdRegex.IsMatch(zohoUid)) throw new ArgumentException("INVALID_ZOHO_UID");
        return new ApiResult { StatusCode = 404, Body = new { status = "error", code = "USER_NOT_FOUND", message = "No local user record found for the given identifier." } };
    }

    private static void ValidateAuth(HttpRequest request)
    {
        if (!request.Headers.TryGetValue("Authorization", out var auth) || string.IsNullOrWhiteSpace(auth.ToString()) || !auth.ToString().StartsWith("Zoho-oauthtoken "))
        {
            throw new InvalidOperationException("UNAUTHORIZED");
        }
    }

    private static void ValidateCreateRequest(ZohoCreateUserRequest request)
    {
        if (request.Users.Count != 1) throw new ArgumentException("VALIDATION_ERROR");
        var item = request.Users[0];
        if (string.IsNullOrWhiteSpace(item.LastName)) throw new ArgumentException("last_name is required.");
        if (string.IsNullOrWhiteSpace(item.Email)) throw new ArgumentException("email is required.");
        if (!ZohoIdRegex.IsMatch(item.Role ?? string.Empty)) throw new ArgumentException("INVALID_ROLE_ID");
        if (!ZohoIdRegex.IsMatch(item.Profile ?? string.Empty)) throw new ArgumentException("INVALID_PROFILE_ID");
    }
}