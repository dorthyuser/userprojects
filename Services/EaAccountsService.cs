using System.Text.Json;

namespace BusTravelAccountsLambda;

public sealed class EaAccountsService
{
    private static readonly PaApiService PaApi = new();

    public async Task<EaApiResult> GetAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers, string? body)
    {
        var email = HeaderHelper.GetQuery(query, "email")?.Trim() ?? string.Empty;
        var result = await PaApi.GetAccountsAsync(new Dictionary<string, string> { ["email"] = email }, HeaderHelper.Filter(headers, "client_id", "client_secret"));
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = result.Headers };
    }

    public async Task<EaApiResult> GetAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers, string? body)
    {
        var result = await PaApi.GetAccountByIdAsync(id.Trim(), query, HeaderHelper.Filter(headers, "client_id", "client_secret"));
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = result.Headers };
    }

    public async Task<EaApiResult> PostAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers, string? body)
    {
        var result = await PaApi.PostAccountsAsync(query, HeaderHelper.Filter(headers, "client_id", "client_secret"), body);
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = result.Headers };
    }

    public async Task<EaApiResult> PutAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers, string? body)
    {
        var result = await PaApi.PutAccountByIdAsync(id.Trim(), query, HeaderHelper.Filter(headers, "client_id", "client_secret"), body);
        return new EaApiResult { StatusCode = result.StatusCode, Body = result.Body, Headers = result.Headers };
    }
}
