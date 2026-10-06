using Microsoft.AspNetCore.Http;

namespace BusTravelAccountsEaMainFunction;

public sealed class AccountsService
{
    private readonly AccountsClient _client;
    private readonly Settings _settings;

    public AccountsService(Settings settings)
    {
        _settings = settings;
        _client = new AccountsClient(settings);
    }

    public Task<DownstreamResponse> GetAccountsAsync(IQueryCollection query, string correlationId, CancellationToken cancellationToken)
    {
        var email = query["email"].ToString().Trim();
        return _client.GetAccountsAsync(email, correlationId, cancellationToken);
    }

    public Task<DownstreamResponse> GetAccountByIdAsync(string id, string correlationId, CancellationToken cancellationToken)
    {
        return _client.GetAccountByIdAsync(id.Trim(), correlationId, cancellationToken);
    }

    public Task<DownstreamResponse> CreateAccountAsync(string body, string correlationId, CancellationToken cancellationToken)
    {
        return _client.CreateAccountAsync(body, correlationId, cancellationToken);
    }

    public Task<DownstreamResponse> UpdateAccountAsync(string id, string body, string correlationId, CancellationToken cancellationToken)
    {
        return _client.UpdateAccountAsync(id.Trim(), body, correlationId, cancellationToken);
    }
}