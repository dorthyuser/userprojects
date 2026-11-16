using System.Text.Json.Nodes;

namespace BusTravelAccountsSaFunction;

public interface IAccountsRepository
{
    Task<IReadOnlyList<JsonObject>> GetAccountsByEmailAsync(string? email, CancellationToken cancellationToken);

    Task<IReadOnlyList<JsonObject>> GetAccountByIdAsync(string id, CancellationToken cancellationToken);

    Task<SalesforceBulkResult> CreateAccountAsync(JsonObject record, CancellationToken cancellationToken);

    Task<SalesforceBulkResult> UpdateAccountAsync(JsonObject record, CancellationToken cancellationToken);
}
