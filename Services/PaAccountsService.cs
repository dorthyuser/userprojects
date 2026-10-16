namespace BusTravelAccountsLambda;

public sealed class PaAccountsService
{
    private static readonly SaApiService Sa = new();

    public async Task<PaApiResult> GetAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var email = query != null && query.TryGetValue("email", out var e) ? e?.Trim() : null;
        AppLog.Info($"Before Request - bus-travel-accounts-sa-get-accounts email={email}");
        try
        {
            var result = await Sa.GetAccountsAsync(new Dictionary<string, string> { ["email"] = email ?? string.Empty }, headers);
            AppLog.Info($"After Request - bus-travel-accounts-sa-get-accounts status={result.StatusCode}");
            return new PaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return new PaApiResult { StatusCode = 500, Body = JsonHelper.Serialize(new PaErrorResponse { Error = new PaErrorBody { ErrorCode = 500, ErrorMessage = "MULE UNKNOWN ERROR", ErrorDescription = "An unexpected error occurred" } }), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
    }

    public async Task<PaApiResult> GetAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var accountId = string.IsNullOrWhiteSpace(id) ? null : id.Trim();
        AppLog.Info($"Before Request - bus-travel-accounts-sa-get-account-details id={accountId}");
        try
        {
            var result = await Sa.GetAccountByIdAsync(accountId ?? string.Empty, query, headers);
            AppLog.Info($"After Request - bus-travel-accounts-sa-get-account-details status={result.StatusCode}");
            return new PaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return new PaApiResult { StatusCode = 500, Body = JsonHelper.Serialize(new PaErrorResponse { Error = new PaErrorBody { ErrorCode = 500, ErrorMessage = "MULE UNKNOWN ERROR", ErrorDescription = "An unexpected error occurred" } }), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
    }

    public async Task<PaApiResult> PostAccountsAsync(string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        AppLog.Info("Before Request - bus-travel-accounts-sa-get-accounts (duplicate check)");
        try
        {
            var request = JsonHelper.Deserialize<SaAccountRequest>(body);
            var email = request?.PersonEmail?.Trim().ToLowerInvariant() ?? string.Empty;
            var existing = await Sa.GetAccountsAsync(new Dictionary<string, string> { ["email"] = email }, headers);
            if (existing.StatusCode == 200 && (JsonHelper.Deserialize<List<object>>(existing.Body) ?? new List<object>()).Count > 0)
            {
                return new PaApiResult { StatusCode = 409, Body = JsonHelper.Serialize(new PaErrorResponse { Error = new PaErrorBody { ErrorCode = 409, ErrorMessage = "CONFLICT", ErrorDescription = "An account with this email already exists" } }), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
            }
            if (existing.StatusCode != 200 && existing.StatusCode != 204)
            {
                return new PaApiResult { StatusCode = existing.StatusCode, Body = existing.Body, Headers = new Dictionary<string, string>(existing.Headers, StringComparer.OrdinalIgnoreCase) };
            }
            AppLog.Info("Before Request - bus-travel-accounts-sa-create-Accounts");
            var created = await Sa.PostAccountsAsync(body, query, headers);
            AppLog.Info($"After Request - bus-travel-accounts-sa-create-Accounts status={created.StatusCode}");
            return new PaApiResult { StatusCode = created.StatusCode, Body = created.Body, Headers = new Dictionary<string, string>(created.Headers, StringComparer.OrdinalIgnoreCase) };
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return new PaApiResult { StatusCode = 500, Body = JsonHelper.Serialize(new PaErrorResponse { Error = new PaErrorBody { ErrorCode = 500, ErrorMessage = "MULE UNKNOWN ERROR", ErrorDescription = "An unexpected error occurred" } }), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
    }

    public async Task<PaApiResult> PutAccountByIdAsync(string id, string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var accountId = string.IsNullOrWhiteSpace(id) ? null : id.Trim();
        AppLog.Info($"Before Request - bus-travel-accounts-sa-get-account-details (pre-update check) id={accountId}");
        try
        {
            var existing = await Sa.GetAccountByIdAsync(accountId ?? string.Empty, query, headers);
            if (existing.StatusCode == 204)
            {
                return new PaApiResult { StatusCode = 404, Body = JsonHelper.Serialize(new PaErrorResponse { Error = new PaErrorBody { ErrorCode = 404, ErrorMessage = "RESOURCE NOT FOUND", ErrorDescription = "Account not found" } }), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
            }
            if (existing.StatusCode == 200 && JsonHelper.Deserialize<Dictionary<string, object>>(existing.Body)?.TryGetValue("hotlisted", out var hotlisted) == true && hotlisted is bool b && b)
            {
                return new PaApiResult { StatusCode = 422, Body = JsonHelper.Serialize(new PaErrorResponse { Error = new PaErrorBody { ErrorCode = 422, ErrorMessage = "UNPROCESSABLE ENTITY", ErrorDescription = "Account is hotlisted and cannot be changed" } }), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
            }
            if (existing.StatusCode != 200)
            {
                return new PaApiResult { StatusCode = existing.StatusCode, Body = existing.Body, Headers = new Dictionary<string, string>(existing.Headers, StringComparer.OrdinalIgnoreCase) };
            }
            AppLog.Info("Before Request - bus-travel-accounts-sa-update-account");
            var updated = await Sa.PutAccountByIdAsync(accountId ?? string.Empty, body, query, headers);
            AppLog.Info($"After Request - bus-travel-accounts-sa-update-account status={updated.StatusCode}");
            return new PaApiResult { StatusCode = updated.StatusCode, Body = updated.Body, Headers = new Dictionary<string, string>(updated.Headers, StringComparer.OrdinalIgnoreCase) };
        }
        catch (Exception ex)
        {
            AppLog.Error("Unexpected error", ex);
            return new PaApiResult { StatusCode = 500, Body = JsonHelper.Serialize(new PaErrorResponse { Error = new PaErrorBody { ErrorCode = 500, ErrorMessage = "MULE UNKNOWN ERROR", ErrorDescription = "An unexpected error occurred" } }), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
    }
}