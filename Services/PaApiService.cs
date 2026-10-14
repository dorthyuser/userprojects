namespace BusTravelAccountsLambda;

public sealed class PaApiService
{
    private static readonly SaApiService Sa = new();
    public PaApiService() { }
    /// <summary>GET /accounts</summary>
    public async Task<PaApiResult> GetAccountsAsync(string? email, IDictionary<string, string>? headers)
    {
        var result = await Sa.GetAccountsAsync(email, headers);
        return new PaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = result.Headers };
    }
    /// <summary>GET /accounts/{id}</summary>
    public async Task<PaApiResult> GetAccountDetailsAsync(string id, IDictionary<string, string>? headers)
    {
        var result = await Sa.GetAccountDetailsAsync(id, headers);
        return new PaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = result.Headers };
    }
    /// <summary>POST /accounts</summary>
    public async Task<PaApiResult> PostAccountsAsync(string? body, IDictionary<string, string>? headers)
    {
        var duplicate = await Sa.GetAccountsAsync(ExtractPersonEmail(body), headers);
        if (duplicate.StatusCode == 200 && !string.IsNullOrWhiteSpace(duplicate.Body) && duplicate.Body != "[]") return new PaApiResult { StatusCode = 409, Body = System.Text.Json.JsonSerializer.Serialize(new PaErrorResponse { Error = new PaErrorBody { ErrorCode = 409, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "CONFLICT", ErrorDescription = "An account with this email already exists" } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        if (duplicate.StatusCode != 200 && duplicate.StatusCode != 204) return new PaApiResult { StatusCode = duplicate.StatusCode, Body = duplicate.Body, Headers = duplicate.Headers };
        var created = await Sa.PostAccountsAsync(body, headers);
        return new PaApiResult { StatusCode = created.StatusCode, Body = created.Body, Headers = created.Headers };
    }
    /// <summary>PUT /accounts/{id}</summary>
    public async Task<PaApiResult> PutAccountsAsync(string id, string? body, IDictionary<string, string>? headers)
    {
        var existing = await Sa.GetAccountDetailsAsync(id, headers);
        if (existing.StatusCode == 204) return new PaApiResult { StatusCode = 404, Body = System.Text.Json.JsonSerializer.Serialize(new PaErrorResponse { Error = new PaErrorBody { ErrorCode = 404, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "RESOURCE NOT FOUND", ErrorDescription = "Account not found" } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        if (existing.StatusCode == 200 && existing.Body?.Contains("\"hotlisted\":true", StringComparison.OrdinalIgnoreCase) == true) return new PaApiResult { StatusCode = 422, Body = System.Text.Json.JsonSerializer.Serialize(new PaErrorResponse { Error = new PaErrorBody { ErrorCode = 422, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "UNPROCESSABLE ENTITY", ErrorDescription = "Account is hotlisted and cannot be changed" } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        if (existing.StatusCode != 200) return new PaApiResult { StatusCode = existing.StatusCode, Body = existing.Body, Headers = existing.Headers };
        var updated = await Sa.PutAccountsAsync(id, body, headers);
        return new PaApiResult { StatusCode = updated.StatusCode, Body = updated.Body, Headers = updated.Headers };
    }
    /// <summary>ANY /alive</summary>
    public Task<PaApiResult> AliveAsync() => Task.FromResult(new PaApiResult { StatusCode = 200, Body = "UP", Headers = new Dictionary<string, string> { ["Content-Type"] = "text/plain" } });
    /// <summary>ANY /ready</summary>
    public Task<PaApiResult> ReadyAsync() => Task.FromResult(new PaApiResult { StatusCode = 200, Body = "UP", Headers = new Dictionary<string, string> { ["Content-Type"] = "text/plain" } });
    private static string? ExtractPersonEmail(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        using var doc = System.Text.Json.JsonDocument.Parse(body);
        return doc.RootElement.TryGetProperty("personEmail", out var v) ? v.GetString()?.Trim().ToLowerInvariant() : null;
    }
}