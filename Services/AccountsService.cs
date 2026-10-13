using System.Globalization;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;

namespace BusTravelAccountsSaLambda;

public sealed class AccountsService
{
    private readonly Settings _settings;
    private readonly AccountsClient _client;

    public AccountsService(Settings settings)
    {
        _settings = settings;
        _client = new AccountsClient(settings);
    }

    public async Task<ApiResult> HandleAsync(string method, string path, IDictionary<string, string>? query, IDictionary<string, string>? headers, string? body, CancellationToken cancellationToken)
    {
        var normalizedPath = NormalizePath(path, _settings.ListenerPath);
        if (normalizedPath == "/alive")
        {
            return ApiResult.Text(200, "UP");
        }

        if (normalizedPath == "/ready")
        {
            try
            {
                await _client.CheckReadyAsync(cancellationToken);
                return ApiResult.Text(200, "UP");
            }
            catch (Exception ex)
            {
                AppLog.Error("Readiness check failed", ex);
                return ErrorBodies.Down();
            }
        }

        try
        {
            if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) && normalizedPath == "/accounts")
            {
                var email = query is not null && query.TryGetValue("email", out var value) ? value : string.Empty;
                var rows = await _client.GetAccountsByEmailAsync(email ?? string.Empty, cancellationToken);
                if (rows.Count == 0)
                {
                    return ApiResult.Json(204, "[]");
                }

                return ApiResult.Json(200, JsonSerializer.Serialize(rows.Select(AccountResponse.FromRow).ToArray(), JsonOptions.Default));
            }

            if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) && normalizedPath.StartsWith("/accounts/", StringComparison.OrdinalIgnoreCase))
            {
                var idText = normalizedPath["/accounts/".Length..];
                if (!Guid.TryParse(idText, out var id))
                {
                    return ApiResult.Json(204, "[]");
                }

                var row = await _client.GetAccountByIdAsync(id, cancellationToken);
                if (row is null)
                {
                    return ApiResult.Json(204, "[]");
                }

                return ApiResult.Json(200, JsonSerializer.Serialize(AccountResponse.FromRow(row), JsonOptions.Default));
            }

            if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) && normalizedPath == "/accounts")
            {
                var request = JsonSerializer.Deserialize<CreateAccountRequest>(body ?? "{}", JsonOptions.Default) ?? new CreateAccountRequest();
                var result = await _client.CreateAccountAsync(request, cancellationToken);
                return ApiResult.Json(200, JsonSerializer.Serialize(new { id = result }, JsonOptions.Default));
            }

            if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase) && normalizedPath.StartsWith("/accounts/", StringComparison.OrdinalIgnoreCase))
            {
                var idText = normalizedPath["/accounts/".Length..];
                if (!Guid.TryParse(idText, out var id))
                {
                    return ErrorBodies.AccountNotFound();
                }

                var request = JsonSerializer.Deserialize<UpdateAccountRequest>(body ?? "{}", JsonOptions.Default) ?? new UpdateAccountRequest();
                var updated = await _client.UpdateAccountAsync(id, request, cancellationToken);
                if (!updated)
                {
                    return ErrorBodies.AccountNotFound();
                }

                return ApiResult.Json(200, JsonSerializer.Serialize(new { id }, JsonOptions.Default));
            }

            return ErrorBodies.NotFound();
        }
        catch (PostgresOperationException ex)
        {
            AppLog.Error("Database operation failed", ex);
            return ErrorBodies.DatabaseError(ex.SqlState, ex.Message);
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return ErrorBodies.Unexpected();
        }
    }

    private static string NormalizePath(string path, string basePath)
    {
        var p = path ?? string.Empty;
        var prefix = basePath.TrimEnd('*').TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(prefix) && p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            p = p[prefix.Length..];
        }

        if (string.IsNullOrEmpty(p))
        {
            return "/";
        }

        return p.StartsWith('/') ? p : "/" + p;
    }
}
