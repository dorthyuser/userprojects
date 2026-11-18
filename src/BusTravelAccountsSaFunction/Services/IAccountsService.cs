using Microsoft.AspNetCore.Mvc;

namespace BusTravelAccountsSaFunction;

public interface IAccountsService
{
    Task<IActionResult> GetAccountsAsync(string? email, CancellationToken cancellationToken);
    Task<IActionResult> GetAccountByIdAsync(string id, CancellationToken cancellationToken);
    Task<IActionResult> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken);
    Task<IActionResult> UpdateAccountAsync(string id, UpdateAccountRequest request, CancellationToken cancellationToken);
}