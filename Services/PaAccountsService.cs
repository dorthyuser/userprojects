using System.Text.Json;

namespace BusTravelAccountsLambda;

public sealed class PaAccountsService
{
    private static readonly SaApiService Api = new();

    public async Task<PaApiResult> GetAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var result = await Api.GetAccountsAsync(query, headers);
        return new PaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
    }

    public async Task<PaApiResult> GetAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var result = await Api.GetAccountByIdAsync(id, query, headers);
        return new PaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
    }

    public async Task<PaApiResult> PostAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers, string? body)
    {
        var result = await Api.PostAccountsAsync(query, headers, body);
        return new PaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
    }

    public async Task<PaApiResult> PutAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers, string? body)
    {
        var result = await Api.PutAccountByIdAsync(id, query, headers, body);
        return new PaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
    }
}