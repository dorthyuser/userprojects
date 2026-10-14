using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace BusTravelAccountsLambda;

public sealed class EaAccountsService
{
    private static readonly PaApiService Pa = new();

    public async Task<EaApiResult> GetAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var result = await Pa.GetAccountsAsync(query, headers);
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
    }

    public async Task<EaApiResult> GetAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var result = await Pa.GetAccountByIdAsync(id, query, headers);
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
    }

    public async Task<EaApiResult> PostAccountsAsync(string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var result = await Pa.PostAccountsAsync(body, query, headers);
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
    }

    public async Task<EaApiResult> PutAccountByIdAsync(string id, string? body, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var result = await Pa.PutAccountByIdAsync(id, body, query, headers);
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
    }

    public Task<EaApiResult> AliveAsync() => Task.FromResult(new EaApiResult { StatusCode = 200, Body = "UP", Headers = ResponseHelper.TextHeaders() });

    public Task<EaApiResult> ReadyAsync() => Task.FromResult(new EaApiResult { StatusCode = 200, Body = "UP", Headers = ResponseHelper.TextHeaders() });
}