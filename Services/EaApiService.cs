using System.Collections.Generic;

namespace BusTravelAccountsLambda;

public sealed class EaApiService
{
    private static readonly EaService Service = new();

    public EaApiService() { }

    /// <summary>GET /accounts</summary>
    public Task<EaApiResult> GetAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers)
        => Service.GetAccountsAsync(query, headers);

    /// <summary>GET /accounts/{id}</summary>
    public Task<EaApiResult> GetAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers)
        => Service.GetAccountByIdAsync(id, query, headers);

    /// <summary>POST /accounts</summary>
    public Task<EaApiResult> PostAccountsAsync(string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
        => Service.PostAccountsAsync(body, query, headers);

    /// <summary>PUT /accounts/{id}</summary>
    public Task<EaApiResult> PutAccountByIdAsync(string id, string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
        => Service.PutAccountByIdAsync(id, body, query, headers);

    /// <summary>ANY /alive</summary>
    public Task<EaApiResult> AliveAsync() => Task.FromResult(new EaApiResult { StatusCode = 200, Body = "UP", Headers = new Dictionary<string, string> { ["Content-Type"] = "text/plain" } });

    /// <summary>ANY /ready</summary>
    public Task<EaApiResult> ReadyAsync() => Task.FromResult(new EaApiResult { StatusCode = 200, Body = "UP", Headers = new Dictionary<string, string> { ["Content-Type"] = "text/plain" } });
}
