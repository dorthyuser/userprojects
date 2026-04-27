using test_sf_git_prop.Models;

namespace test_sf_git_prop.Services;

public interface IAccountService
{
    Task<IReadOnlyCollection<AccountDto>> GetAccountsAsync(CancellationToken cancellationToken);
    Task<AccountDto> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken);
    Task<AccountDto> UpdateAccountAsync(string id, UpdateAccountRequest request, CancellationToken cancellationToken);
}