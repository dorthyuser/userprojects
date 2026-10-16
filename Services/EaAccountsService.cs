namespace BusTravelAccountsLambda;

public sealed class EaAccountsService
{
    private readonly PaApiService _pa;
    public EaAccountsService(PaApiService pa) => _pa = pa;

    public async Task<EaApiResult> GetAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var email = query != null && query.TryGetValue("email", out var e) ? e?.Trim() : null;
        AppLog.Info("Before Request - bus-travel-accounts-pa-get-accounts");
        var result = await _pa.GetAccountsAsync(new Dictionary<string, string> { ["email"] = email ?? string.Empty }, headers);
        AppLog.Info($"After Request - bus-travel-accounts-pa-get-accounts status={result.StatusCode}");
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
    }

    public async Task<EaApiResult> GetAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var accountId = string.IsNullOrWhiteSpace(id) ? null : id.Trim();
        AppLog.Info($"Before Request - bus-travel-accounts-pa-get-account-details id={accountId}");
        var result = await _pa.GetAccountByIdAsync(accountId ?? string.Empty, query, headers);
        AppLog.Info($"After Request - bus-travel-accounts-pa-get-account-details status={result.StatusCode}");
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
    }

    public async Task<EaApiResult> PostAccountsAsync(string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        AppLog.Info("Before Request - bus-travel-accounts-pa-create-Accounts");
        var result = await _pa.PostAccountsAsync(body, query, headers);
        AppLog.Info($"After Request - bus-travel-accounts-pa-create-Accounts status={result.StatusCode}");
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
    }

    public async Task<EaApiResult> PutAccountByIdAsync(string id, string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var accountId = string.IsNullOrWhiteSpace(id) ? null : id.Trim();
        AppLog.Info($"Before Request - bus-travel-accounts-pa-update-account id={accountId}");
        var result = await _pa.PutAccountByIdAsync(accountId ?? string.Empty, body, query, headers);
        AppLog.Info($"After Request - bus-travel-accounts-pa-update-account status={result.StatusCode}");
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
    }
}