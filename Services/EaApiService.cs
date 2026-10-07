using System.Text.Json;

namespace MuleaesaMainLambda;

public sealed class EaApiService
{
    private static readonly SaApiService SaApiService = new();

    public Task<EaApiResult> GetAliveAsync()
    {
        return Task.FromResult(new EaApiResult
        {
            StatusCode = 200,
            Body = "UP",
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        });
    }

    public Task<EaApiResult> GetReadyAsync()
    {
        return Task.FromResult(new EaApiResult
        {
            StatusCode = 200,
            Body = "UP",
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        });
    }

    public async Task<EaApiResult> GetAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var result = await SaApiService.GetAccountsAsync(query, headers).ConfigureAwait(false);
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = result.Headers };
    }

    public async Task<EaApiResult> GetAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var result = await SaApiService.GetAccountByIdAsync(id, query, headers).ConfigureAwait(false);
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = result.Headers };
    }

    public async Task<EaApiResult> CreateAccountAsync(string? body, IDictionary<string, string>? headers)
    {
        var result = await SaApiService.CreateAccountAsync(body, headers).ConfigureAwait(false);
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = result.Headers };
    }

    public async Task<EaApiResult> UpdateAccountAsync(string id, string? body, IDictionary<string, string>? headers)
    {
        var result = await SaApiService.UpdateAccountAsync(id, body, headers).ConfigureAwait(false);
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = result.Headers };
    }
}
