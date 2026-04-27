using test_sf_git_prop.Models;

namespace test_sf_git_prop.Services;

public sealed class AccountService : IAccountService
{
    private readonly ISalesforceClient _salesforceClient;

    public AccountService(ISalesforceClient salesforceClient)
    {
        _salesforceClient = salesforceClient;
    }

    public Task<IReadOnlyCollection<AccountDto>> GetAccountsAsync(CancellationToken cancellationToken) => _salesforceClient.GetAccountsAsync(cancellationToken);

    public Task<AccountDto> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required.");
        }

        return _salesforceClient.CreateAccountAsync(request, cancellationToken);
    }

    public Task<AccountDto> UpdateAccountAsync(string id, UpdateAccountRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Account id is required.");
        }

        return _salesforceClient.UpdateAccountAsync(id, request, cancellationToken);
    }
}