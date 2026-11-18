using System.Threading;
using System.Threading.Tasks;

namespace BusTravelAccountsSaFunction;

public interface IAccountsRepository
{
    Task<IReadOnlyList<AccountResponse>> GetByEmailAsync(string? email, CancellationToken cancellationToken);
    Task<AccountResponse?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<string> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken);
    Task<string> UpdateAsync(string id, UpdateAccountRequest request, CancellationToken cancellationToken);
}