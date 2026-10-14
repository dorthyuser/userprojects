using System.Collections.Generic;

namespace BusTravelAccountsLambda;

public sealed class EaService
{
    private static readonly PaApiService Pa = new();

    public async Task<EaApiResult> GetAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var email = query != null && query.TryGetValue("email", out var e) ? e?.Trim() : string.Empty;
        var result = await Pa.GetAccountsAsync(new Dictionary<string, string> { ["email"] = email ?? string.Empty }, headers);
        return Copy(result);
    }

    public async Task<EaApiResult> GetAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var result = await Pa.GetAccountByIdAsync(id.Trim(), query, headers);
        return Copy(result);
    }

    public async Task<EaApiResult> PostAccountsAsync(string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var result = await Pa.PostAccountsAsync(body, query, headers);
        return Copy(result);
    }

    public async Task<EaApiResult> PutAccountByIdAsync(string id, string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var result = await Pa.PutAccountByIdAsync(id.Trim(), body, query, headers);
        return Copy(result);
    }

    public Task<EaApiResult> ReadyAsync() => Task.FromResult(new EaApiResult { StatusCode = 200, Body = "UP", Headers = new Dictionary<string, string> { ["Content-Type"] = "text/plain" } });
    public Task<EaApiResult> AliveAsync() => Task.FromResult(new EaApiResult { StatusCode = 200, Body = "UP", Headers = new Dictionary<string, string> { ["Content-Type"] = "text/plain" } });

    private static EaApiResult Copy(PaApiResult result) => new() { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
}
