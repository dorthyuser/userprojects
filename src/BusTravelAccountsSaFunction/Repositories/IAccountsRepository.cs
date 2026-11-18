namespace BusTravelAccountsSaFunction;

public interface IAccountsRepository
{
    Task<IReadOnlyList<AccountRecord>> GetByEmailAsync(string? email, CancellationToken cancellationToken);
    Task<AccountRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken);
    Task<Guid> UpdateAsync(Guid id, UpdateAccountRequest request, CancellationToken cancellationToken);
    Task CheckHealthAsync(CancellationToken cancellationToken);
}