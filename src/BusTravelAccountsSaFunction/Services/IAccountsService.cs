namespace BusTravelAccountsSaFunction;

public interface IAccountsService
{
    Task<IReadOnlyList<AccountResponse>> GetAccountsAsync(string? email, CancellationToken cancellationToken);

    Task<AccountResponse?> GetAccountByIdAsync(string id, CancellationToken cancellationToken);

    Task<AccountIdResponse> CreateAccountAsync(AccountUpsertRequest request, CancellationToken cancellationToken);

    Task<AccountIdResponse> UpdateAccountAsync(string id, AccountUpsertRequest request, CancellationToken cancellationToken);
}
