using System.Text.Json;

namespace BusTravelAccountsLambda;

public sealed class SaAccountsService
{
    private static readonly SaSalesforceClient Client = new();

    public async Task<SaApiResult> GetAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var email = HeaderHelper.GetQuery(query, "email")?.Trim() ?? string.Empty;
        var result = await Client.QueryAccountsByEmailAsync(email);
        return result;
    }

    public async Task<SaApiResult> GetAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers)
    {
        var accountId = id?.Trim() ?? string.Empty;
        var result = await Client.QueryAccountByIdAsync(accountId);
        return result;
    }

    public async Task<SaApiResult> PostAccountsAsync(IDictionary<string, string>? query, IDictionary<string, string>? headers, string? body)
    {
        var request = JsonSerializer.Deserialize<SaAccountRequest>(body ?? string.Empty, JsonOptions.Default) ?? throw new InvalidOperationException("check payload existence or method/payload combination requirement");
        var result = await Client.CreateAccountAsync(request);
        return result;
    }

    public async Task<SaApiResult> PutAccountByIdAsync(string id, IDictionary<string, string>? query, IDictionary<string, string>? headers, string? body)
    {
        var request = JsonSerializer.Deserialize<SaAccountRequest>(body ?? string.Empty, JsonOptions.Default) ?? throw new InvalidOperationException("check payload existence or method/payload combination requirement");
        var result = await Client.UpdateAccountAsync(id.Trim(), request);
        return result;
    }
}