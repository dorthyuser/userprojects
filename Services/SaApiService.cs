namespace BusTravelAccountsLambda;

public sealed class SaApiService
{
    private static readonly SaPostgresClient Client = new();
    public SaApiService() { }
    /// <summary>GET /accounts</summary>
    public Task<SaApiResult> GetAccountsAsync(string? email, IDictionary<string, string>? headers) => Client.GetAccountsAsync(email);
    /// <summary>GET /accounts/{id}</summary>
    public Task<SaApiResult> GetAccountDetailsAsync(string id, IDictionary<string, string>? headers) => Client.GetAccountDetailsAsync(id);
    /// <summary>POST /accounts</summary>
    public Task<SaApiResult> PostAccountsAsync(string? body, IDictionary<string, string>? headers) => Client.PostAccountsAsync(body);
    /// <summary>PUT /accounts/{id}</summary>
    public Task<SaApiResult> PutAccountsAsync(string id, string? body, IDictionary<string, string>? headers) => Client.PutAccountsAsync(id, body);
    /// <summary>ANY /alive</summary>
    public Task<SaApiResult> AliveAsync() => Task.FromResult(new SaApiResult { StatusCode = 200, Body = "UP", Headers = new Dictionary<string, string> { ["Content-Type"] = "text/plain" } });
    /// <summary>ANY /ready</summary>
    public Task<SaApiResult> ReadyAsync() => Task.FromResult(new SaApiResult { StatusCode = 200, Body = "UP", Headers = new Dictionary<string, string> { ["Content-Type"] = "text/plain" } });
}