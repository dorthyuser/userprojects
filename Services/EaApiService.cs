namespace BusTravelAccountsLambda;

public sealed class EaApiService
{
    private static readonly PaApiService Pa = new();
    public EaApiService() { }
    /// <summary>GET /accounts</summary>
    public async Task<EaApiResult> GetAccountsAsync(string? email, IDictionary<string, string>? headers)
    {
        var result = await Pa.GetAccountsAsync(email, headers);
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = result.Headers };
    }
    /// <summary>GET /accounts/{id}</summary>
    public async Task<EaApiResult> GetAccountDetailsAsync(string id, IDictionary<string, string>? headers)
    {
        var result = await Pa.GetAccountDetailsAsync(id, headers);
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = result.Headers };
    }
    /// <summary>POST /accounts</summary>
    public async Task<EaApiResult> PostAccountsAsync(string? body, IDictionary<string, string>? headers)
    {
        var result = await Pa.PostAccountsAsync(body, headers);
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = result.Headers };
    }
    /// <summary>PUT /accounts/{id}</summary>
    public async Task<EaApiResult> PutAccountsAsync(string id, string? body, IDictionary<string, string>? headers)
    {
        var result = await Pa.PutAccountsAsync(id, body, headers);
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = result.Headers };
    }
    /// <summary>ANY /alive</summary>
    public Task<EaApiResult> AliveAsync() => Task.FromResult(new EaApiResult { StatusCode = 200, Body = "UP", Headers = new Dictionary<string, string> { ["Content-Type"] = "text/plain" } });
    /// <summary>ANY /ready</summary>
    public Task<EaApiResult> ReadyAsync() => Task.FromResult(new EaApiResult { StatusCode = 200, Body = "UP", Headers = new Dictionary<string, string> { ["Content-Type"] = "text/plain" } });
}