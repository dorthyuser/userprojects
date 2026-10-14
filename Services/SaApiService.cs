using System.Collections.Generic;

namespace BusTravelAccountsLambda;

public sealed class SaApiService
{
    private static readonly SaService Service = new();

    public SaApiService() { }

    /// <summary>GET /accounts</summary>
    public Task<SaApiResult> GetAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers)
        => Service.GetAccountsAsync(query, headers);

    /// <summary>GET /accounts/{id}</summary>
    public Task<SaApiResult> GetAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers)
        => Service.GetAccountByIdAsync(id, query, headers);

    /// <summary>POST /accounts</summary>
    public Task<SaApiResult> PostAccountsAsync(string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
        => Service.PostAccountsAsync(body, query, headers);

    /// <summary>PUT /accounts/{id}</summary>
    public Task<SaApiResult> PutAccountByIdAsync(string id, string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
        => Service.PutAccountByIdAsync(id, body, query, headers);

    /// <summary>ANY /alive</summary>
    public Task<SaApiResult> AliveAsync() => Task.FromResult(new SaApiResult { StatusCode = 200, Body = "UP", Headers = new Dictionary<string, string> { ["Content-Type"] = "text/plain" } });

    /// <summary>ANY /ready</summary>
    public Task<SaApiResult> ReadyAsync() => Service.ReadyAsync();
}
