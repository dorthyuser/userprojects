using Microsoft.Extensions.Logging;
using Npgsql;

namespace BusTravelAccountsSaFunction;

public sealed class AccountsService(IAccountsRepository repository, ILogger<AccountsService> logger) : IAccountsService
{
    public async Task<ServiceResult> GetAccountsAsync(string? email, CancellationToken cancellationToken)
    {
        var accounts = await repository.GetByEmailAsync(email, cancellationToken);
        if (accounts.Count == 0)
        {
            return ServiceResult.NoContentArray();
        }

        return ServiceResult.Ok(accounts);
    }

    public async Task<ServiceResult> GetAccountByIdAsync(string id, CancellationToken cancellationToken)
    {
        var account = await repository.GetByIdAsync(id, cancellationToken);
        if (account is null)
        {
            return ServiceResult.NoContentArray();
        }

        return ServiceResult.Ok(account);
    }

    public async Task<ServiceResult> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var createdId = await repository.CreateAsync(request, cancellationToken);
            return ServiceResult.Ok(new AccountMutationResponse { Id = createdId });
        }
        catch (PostgresException ex)
        {
            logger.LogError(ex, "Create account failed");
            throw new DatabaseWriteException(ex.SqlState ?? "UNKNOWN", ex);
        }
    }

    public async Task<ServiceResult> UpdateAccountAsync(string id, UpdateAccountRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var updatedId = await repository.UpdateAsync(id, request, cancellationToken);
            return ServiceResult.Ok(new AccountMutationResponse { Id = updatedId });
        }
        catch (PostgresException ex)
        {
            logger.LogError(ex, "Update account failed for account id route parameter");
            throw new DatabaseWriteException(ex.SqlState ?? "UNKNOWN", ex);
        }
    }
}