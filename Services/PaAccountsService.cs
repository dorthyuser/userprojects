using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace BusTravelAccountsLambda;

public sealed class PaAccountsService
{
    private static readonly SaApiService Sa = new();

    public async Task<PaApiResult> GetAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var email = RequestHelper.GetQuery(query, "email")?.Trim();
        var result = await Sa.GetAccountsAsync(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["email"] = email ?? string.Empty }, headers);
        return new PaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
    }

    public async Task<PaApiResult> GetAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var trimmed = id?.Trim() ?? string.Empty;
        var result = await Sa.GetAccountByIdAsync(trimmed, query, headers);
        return new PaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
    }

    public async Task<PaApiResult> PostAccountsAsync(string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        using var document = JsonDocument.Parse(body ?? "{}");
        var root = document.RootElement;
        var personEmail = root.TryGetProperty("personEmail", out var emailValue) && emailValue.ValueKind == JsonValueKind.String ? emailValue.GetString()?.Trim().ToLowerInvariant() ?? string.Empty : string.Empty;
        var duplicate = await Sa.GetAccountsAsync(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["email"] = personEmail }, headers);
        if (duplicate.StatusCode == 200)
        {
            using var dupDoc = JsonDocument.Parse(duplicate.Body ?? "[]");
            if (dupDoc.RootElement.ValueKind == JsonValueKind.Array && dupDoc.RootElement.GetArrayLength() > 0)
            {
                return new PaApiResult { StatusCode = 409, Body = JsonSerializer.Serialize(new PaErrorResponse { Error = new PaErrorDetails { ErrorCode = 409, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "CONFLICT", ErrorDescription = "An account with this email already exists" } }, JsonHelper.Options), Headers = ResponseHelper.JsonHeaders() };
            }
        }
        else if (duplicate.StatusCode != 204)
        {
            return new PaApiResult { StatusCode = duplicate.StatusCode, Body = duplicate.Body, Headers = new Dictionary<string, string>(duplicate.Headers, StringComparer.OrdinalIgnoreCase) };
        }

        var result = await Sa.PostAccountsAsync(body, query, headers);
        return new PaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
    }

    public async Task<PaApiResult> PutAccountByIdAsync(string id, string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var trimmed = id?.Trim() ?? string.Empty;
        var existing = await Sa.GetAccountByIdAsync(trimmed, query, headers);
        if (existing.StatusCode == 204)
        {
            return new PaApiResult { StatusCode = 404, Body = JsonSerializer.Serialize(new PaErrorResponse { Error = new PaErrorDetails { ErrorCode = 404, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "RESOURCE NOT FOUND", ErrorDescription = "Account not found" } }, JsonHelper.Options), Headers = ResponseHelper.JsonHeaders() };
        }

        if (existing.StatusCode == 200 && existing.Body is not null)
        {
            using var doc = JsonDocument.Parse(existing.Body);
            if (doc.RootElement.TryGetProperty("hotlisted", out var hotlisted) && hotlisted.ValueKind == JsonValueKind.True)
            {
                return new PaApiResult { StatusCode = 422, Body = JsonSerializer.Serialize(new PaErrorResponse { Error = new PaErrorDetails { ErrorCode = 422, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "UNPROCESSABLE ENTITY", ErrorDescription = "Account is hotlisted and cannot be changed" } }, JsonHelper.Options), Headers = ResponseHelper.JsonHeaders() };
            }
        }
        else if (existing.StatusCode != 200)
        {
            return new PaApiResult { StatusCode = existing.StatusCode, Body = existing.Body, Headers = new Dictionary<string, string>(existing.Headers, StringComparer.OrdinalIgnoreCase) };
        }

        var result = await Sa.PutAccountByIdAsync(trimmed, body, query, headers);
        return new PaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
    }

    public Task<PaApiResult> AliveAsync() => Task.FromResult(new PaApiResult { StatusCode = 200, Body = "UP", Headers = ResponseHelper.TextHeaders() });

    public Task<PaApiResult> ReadyAsync() => Task.FromResult(new PaApiResult { StatusCode = 200, Body = "UP", Headers = ResponseHelper.TextHeaders() });
}