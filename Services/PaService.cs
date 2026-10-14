using System.Collections.Generic;
using System.Text.Json;

namespace BusTravelAccountsLambda;

public sealed class PaService
{
    private static readonly SaApiService Sa = new();

    public async Task<PaApiResult> GetAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var email = query != null && query.TryGetValue("email", out var e) ? e?.Trim() : string.Empty;
        var result = await Sa.GetAccountsAsync(new Dictionary<string, string> { ["email"] = email ?? string.Empty }, headers);
        return Copy(result);
    }

    public async Task<PaApiResult> GetAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var result = await Sa.GetAccountByIdAsync(id.Trim(), query, headers);
        return Copy(result);
    }

    public async Task<PaApiResult> PostAccountsAsync(string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var doc = JsonDocument.Parse(body ?? "{}");
        var email = doc.RootElement.TryGetProperty("personEmail", out var p) ? (p.GetString() ?? string.Empty).Trim().ToLowerInvariant() : string.Empty;
        var existing = await Sa.GetAccountsAsync(new Dictionary<string, string> { ["email"] = email }, headers);
        if (existing.StatusCode == 200)
        {
            var arr = JsonDocument.Parse(existing.Body ?? "[]").RootElement;
            if (arr.GetArrayLength() > 0)
            {
                return new PaApiResult { StatusCode = 409, Body = JsonSerializer.Serialize(new { error = new { errorCode = 409, errorDateTime = DateTime.UtcNow, errorMessage = "CONFLICT", errorDescription = "An account with this email already exists" } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
            }
        }
        else if (existing.StatusCode != 204)
        {
            return Copy(existing);
        }
        var created = await Sa.PostAccountsAsync(body, query, headers);
        return Copy(created);
    }

    public async Task<PaApiResult> PutAccountByIdAsync(string id, string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var existing = await Sa.GetAccountByIdAsync(id.Trim(), query, headers);
        if (existing.StatusCode == 204) return new PaApiResult { StatusCode = 404, Body = JsonSerializer.Serialize(new { error = new { errorCode = 404, errorDateTime = DateTime.UtcNow, errorMessage = "RESOURCE NOT FOUND", errorDescription = "Account not found" } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        if (existing.StatusCode == 200)
        {
            var obj = JsonDocument.Parse(existing.Body ?? "{}").RootElement;
            if (obj.TryGetProperty("hotlisted", out var h) && h.ValueKind == JsonValueKind.True)
                return new PaApiResult { StatusCode = 422, Body = JsonSerializer.Serialize(new { error = new { errorCode = 422, errorDateTime = DateTime.UtcNow, errorMessage = "UNPROCESSABLE ENTITY", errorDescription = "Account is hotlisted and cannot be changed" } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
        else
        {
            return Copy(existing);
        }
        var updated = await Sa.PutAccountByIdAsync(id.Trim(), body, query, headers);
        return Copy(updated);
    }

    public Task<PaApiResult> ReadyAsync() => Task.FromResult(new PaApiResult { StatusCode = 200, Body = "UP", Headers = new Dictionary<string, string> { ["Content-Type"] = "text/plain" } });
    public Task<PaApiResult> AliveAsync() => Task.FromResult(new PaApiResult { StatusCode = 200, Body = "UP", Headers = new Dictionary<string, string> { ["Content-Type"] = "text/plain" } });

    private static PaApiResult Copy(SaApiResult result) => new() { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
}
