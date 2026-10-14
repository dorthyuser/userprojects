namespace BusTravelAccountsLambda;

public sealed class EaAccountService
{
    private static readonly EaApiService Api = new();
    public async Task<EaApiResult> GetAccountsAsync(string? email, IDictionary<string, string>? headers) => await Api.GetAccountsAsync(email, headers);
    public async Task<EaApiResult> GetAccountDetailsAsync(string id, IDictionary<string, string>? headers) => await Api.GetAccountDetailsAsync(id, headers);
    public async Task<EaApiResult> PostAccountsAsync(string? body, IDictionary<string, string>? headers) => await Api.PostAccountsAsync(body, headers);
    public async Task<EaApiResult> PutAccountsAsync(string id, string? body, IDictionary<string, string>? headers) => await Api.PutAccountsAsync(id, body, headers);
}