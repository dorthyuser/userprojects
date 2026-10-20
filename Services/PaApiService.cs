namespace BusTravelAccountsLambda;

public sealed class PaApiService
{
    private static readonly PaAccountsService AccountsService = new();

    public PaApiService()
    {
    }

    /// <summary>GET /accounts</summary>
    public async Task<PaApiResult> GetAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        return await AccountsService.GetAccountsAsync(query, headers);
    }

    /// <summary>GET /accounts/{id}</summary>
    public async Task<PaApiResult> GetAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        return await AccountsService.GetAccountByIdAsync(id, query, headers);
    }

    /// <summary>POST /accounts</summary>
    public async Task<PaApiResult> PostAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers, string? body)
    {
        return await AccountsService.PostAccountsAsync(query, headers, body);
    }

    /// <summary>PUT /accounts/{id}</summary>
    public async Task<PaApiResult> PutAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers, string? body)
    {
        return await AccountsService.PutAccountByIdAsync(id, query, headers, body);
    }
}