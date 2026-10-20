using System.Text.Json;

namespace BusTravelAccountsLambda;

public sealed class SaApiService
{
    private static readonly SaAccountsService AccountsService = new();

    public SaApiService()
    {
    }

    /// <summary>GET /accounts</summary>
    public async Task<SaApiResult> GetAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        return await AccountsService.GetAccountsAsync(query, headers);
    }

    /// <summary>GET /accounts/{id}</summary>
    public async Task<SaApiResult> GetAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        return await AccountsService.GetAccountByIdAsync(id, query, headers);
    }

    /// <summary>POST /accounts</summary>
    public async Task<SaApiResult> PostAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers, string? body)
    {
        return await AccountsService.PostAccountsAsync(query, headers, body);
    }

    /// <summary>PUT /accounts/{id}</summary>
    public async Task<SaApiResult> PutAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers, string? body)
    {
        return await AccountsService.PutAccountByIdAsync(id, query, headers, body);
    }
}
