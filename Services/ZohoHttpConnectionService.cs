using System.Net;
using System.Text;
using System.Text.Json;
using Npgsql;
using zohotesting.Models;

namespace zohotesting.Services;

public sealed class ZohoHttpConnectionService : IZohoHttpConnectionService
{
    private readonly IZohoHttpConnectionConnection _connection;
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<ZohoHttpConnectionService> _logger;

    public ZohoHttpConnectionService(IZohoHttpConnectionConnection connection, NpgsqlDataSource dataSource, ILogger<ZohoHttpConnectionService> logger)
    {
        _connection = connection;
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task<ServiceResult> CreateUserAsync(CreateZohoUserRequest request, string? correlationId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[ZohoHttpConnectionService] Entering CreateUserAsync");
        using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        using var response = await _connection.SendAsync(HttpMethod.Post, "/crm/v8/users", content, correlationId == null ? null : new Dictionary<string, string> { ["X-Correlation-Id"] = correlationId }, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogInformation("[ZohoHttpConnectionService] Exiting CreateUserAsync with status {StatusCode}", (int)response.StatusCode);
        return new ServiceResult((int)response.StatusCode, body, correlationId);
    }

    public async Task<ServiceResult> GetZohoUsersAsync(string? zohoId, string? type, int? page, int? perPage, string? ifModifiedSince, string? correlationId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[ZohoHttpConnectionService] Entering GetZohoUsersAsync");
        var path = string.IsNullOrWhiteSpace(zohoId) ? "/crm/v8/users" : $"/crm/v8/users/{zohoId}";
        using var response = await _connection.SendAsync(HttpMethod.Get, path, null, correlationId == null ? null : new Dictionary<string, string> { ["X-Correlation-Id"] = correlationId, ["If-Modified-Since"] = ifModifiedSince ?? string.Empty }, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogInformation("[ZohoHttpConnectionService] Exiting GetZohoUsersAsync with status {StatusCode}", (int)response.StatusCode);
        return new ServiceResult((int)response.StatusCode, body, correlationId);
    }

    public async Task<ServiceResult> SyncUsersAsync(SyncZohoUsersRequest? request, string? correlationId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[ZohoHttpConnectionService] Entering SyncUsersAsync");
        var body = JsonSerializer.Serialize(request ?? new SyncZohoUsersRequest());
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await _connection.SendAsync(HttpMethod.Post, "/crm/v8/users", content, correlationId == null ? null : new Dictionary<string, string> { ["X-Correlation-Id"] = correlationId }, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogInformation("[ZohoHttpConnectionService] Exiting SyncUsersAsync with status {StatusCode}", (int)response.StatusCode);
        return new ServiceResult((int)response.StatusCode, responseBody, correlationId);
    }

    public async Task<ServiceResult> GetLocalUsersAsync(LocalUsersQuery query, string? correlationId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[ZohoHttpConnectionService] Entering GetLocalUsersAsync");
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        var where = new List<string>();
        var parameters = new List<NpgsqlParameter>();
        if (!string.IsNullOrWhiteSpace(query.AccountStatus)) { where.Add("account_status = @account_status"); parameters.Add(new NpgsqlParameter("account_status", query.AccountStatus)); }
        if (!string.IsNullOrWhiteSpace(query.ZohoRoleId)) { where.Add("zoho_role_id = @zoho_role_id"); parameters.Add(new NpgsqlParameter("zoho_role_id", query.ZohoRoleId)); }
        if (!string.IsNullOrWhiteSpace(query.ZohoProfileId)) { where.Add("zoho_profile_id = @zoho_profile_id"); parameters.Add(new NpgsqlParameter("zoho_profile_id", query.ZohoProfileId)); }
        if (query.IsConfirmed.HasValue) { where.Add("is_confirmed = @is_confirmed"); parameters.Add(new NpgsqlParameter("is_confirmed", query.IsConfirmed.Value)); }
        if (query.SyncedAfter.HasValue) { where.Add("local_synced_at >= @synced_after"); parameters.Add(new NpgsqlParameter("synced_after", query.SyncedAfter.Value)); }
        var page = query.Page.GetValueOrDefault(1);
        var pageSize = query.PageSize.GetValueOrDefault(50);
        var orderBy = query.SortBy ?? "family_name";
        var order = string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
        var sql = $"SELECT * FROM crm_users {(where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : string.Empty)} ORDER BY {orderBy} {order} LIMIT @limit OFFSET @offset";
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var p in parameters) cmd.Parameters.Add(p);
        cmd.Parameters.AddWithValue("limit", pageSize);
        cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var rows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync(cancellationToken)) rows.Add(ReadRow(reader));
        await reader.CloseAsync();
        await using var countCmd = conn.CreateCommand();
        countCmd.CommandText = $"SELECT COUNT(*) FROM crm_users {(where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : string.Empty)}";
        foreach (var p in parameters) countCmd.Parameters.Add(new NpgsqlParameter(p.ParameterName, p.Value));
        var total = (long)(await countCmd.ExecuteScalarAsync(cancellationToken) ?? 0L);
        var payload = JsonSerializer.Serialize(new { status = "success", page, page_size = pageSize, total_count = total, users = rows });
        _logger.LogInformation("[ZohoHttpConnectionService] Exiting GetLocalUsersAsync with status 200");
        return new ServiceResult(200, payload, correlationId);
    }

    public async Task<ServiceResult> GetLocalUserByPkAsync(int userPk, string? correlationId, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM crm_users WHERE user_pk = @user_pk";
        cmd.Parameters.AddWithValue("user_pk", userPk);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return new ServiceResult(404, JsonSerializer.Serialize(new { status = "error", code = "USER_NOT_FOUND", message = "No local user record found for the given identifier." }), correlationId);
        var payload = JsonSerializer.Serialize(new { status = "success", user = ReadRow(reader) });
        return new ServiceResult(200, payload, correlationId);
    }

    public async Task<ServiceResult> GetLocalUserByZohoUidAsync(string zohoUid, string? correlationId, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM crm_users WHERE zoho_uid = @zoho_uid";
        cmd.Parameters.AddWithValue("zoho_uid", zohoUid);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return new ServiceResult(404, JsonSerializer.Serialize(new { status = "error", code = "USER_NOT_FOUND", message = "No local user record found for the given identifier." }), correlationId);
        var payload = JsonSerializer.Serialize(new { status = "success", user = ReadRow(reader) });
        return new ServiceResult(200, payload, correlationId);
    }

    private static Dictionary<string, object?> ReadRow(NpgsqlDataReader reader)
    {
        var result = new Dictionary<string, object?>();
        for (var i = 0; i < reader.FieldCount; i++) result[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        return result;
    }
}
