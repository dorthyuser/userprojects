using System.Collections.Generic;

namespace BusTravelAccountsLambda;

public sealed class PaApiService
{
    private static readonly PaService Service = new();

    public PaApiService() { }

    /// <summary>GET /accounts</summary>
    public Task<PaApiResult> GetAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers)
        => Service.GetAccountsAsync(query, headers);

    /// <summary>GET /accounts/{id}</summary>
    public Task<PaApiResult> GetAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers)
        => Service.GetAccountByIdAsync(id, query, headers);

    /// <summary>POST /accounts</summary>
    public Task<PaApiResult> PostAccountsAsync(string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
        => Service.PostAccountsAsync(body, query, headers);

    /// <summary>PUT /accounts/{id}</summary>
    public Task<PaApiResult> PutAccountByIdAsync(string id, string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
        => Service.PutAccountByIdAsync(id, body, query, headers);

    /// <summary>ANY /alive</summary>
    public Task<PaApiResult> AliveAsync() => Task.FromResult(new PaApiResult { StatusCode = 200, Body = "UP", Headers = new Dictionary<string, string> { ["Content-Type"] = "text/plain" } });

    /// <summary>ANY /ready</summary>
    public Task<PaApiResult> ReadyAsync() => Task.FromResult(new PaApiResult { StatusCode = 200, Body = "UP", Headers = new Dictionary<string, string> { ["Content-Type"] = "text/plain" } });
}
