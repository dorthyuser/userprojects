using System.Text.Json.Nodes;

namespace BusTravelAccountsSaFunction;

public interface ISalesforceClient
{
    Task<IReadOnlyList<JsonObject>> QueryAsync(string soql, IReadOnlyDictionary<string, string?> parameters, CancellationToken cancellationToken);

    Task<SalesforceBulkResult> CreateAsync(string type, IReadOnlyList<JsonObject> records, CancellationToken cancellationToken);

    Task<SalesforceBulkResult> UpdateAsync(string type, IReadOnlyList<JsonObject> records, CancellationToken cancellationToken);
}
