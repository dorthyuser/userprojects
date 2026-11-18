using System.Threading;
using System.Threading.Tasks;

namespace BusTravelAccountsSaFunction;

public interface IAccountsService
{
    Task<ServiceResult> GetAccountsAsync(string? email, CancellationToken cancellationToken);
    Task<ServiceResult> GetAccountByIdAsync(string id, CancellationToken cancellationToken);
    Task<ServiceResult> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> UpdateAccountAsync(string id, UpdateAccountRequest request, CancellationToken cancellationToken);
}