using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace BusTravelAccountsSaFunction;

public sealed class AccountsRepository(ISalesforceClient salesforceClient, ILogger<AccountsRepository> logger) : IAccountsRepository
{
    public async Task<IReadOnlyList<JsonObject>> GetAccountsByEmailAsync(string? email, CancellationToken cancellationToken)
    {
        logger.LogDebug("Before Request - ListAccountsByEmail");
        var records = await salesforceClient.QueryAsync(
            "select id, Account_Status__c, Salutation, firstname, lastname, PersonEmail, PersonBirthdate, Phone, PersonMobilePhone, PersonMailingAddress, Hotlisted__c, Source__c from account where PersonEmail = ':EmailId'",
            new Dictionary<string, string?> { ["EmailId"] = email },
            cancellationToken);
        logger.LogDebug("After Request - ListAccountsByEmail");
        return records;
    }

    public async Task<IReadOnlyList<JsonObject>> GetAccountByIdAsync(string id, CancellationToken cancellationToken)
    {
        logger.LogDebug("Before Request - GetAccountDetails");
        var records = await salesforceClient.QueryAsync(
            "select id, Account_Status__c, Salutation, firstname, lastname, PersonEmail, PersonBirthdate, Phone, PersonMobilePhone, PersonMailingAddress, Hotlisted__c, Source__c from account where id = ':AccountId'",
            new Dictionary<string, string?> { ["AccountId"] = id },
            cancellationToken);
        logger.LogDebug("After Request - GetAccountDetails");
        return records;
    }

    public async Task<SalesforceBulkResult> CreateAccountAsync(JsonObject record, CancellationToken cancellationToken)
    {
        logger.LogDebug("Before Request - Create single account");
        var result = await salesforceClient.CreateAsync("Account", new List<JsonObject> { record }, cancellationToken);
        logger.LogDebug("After Request - Create single account");
        return result;
    }

    public async Task<SalesforceBulkResult> UpdateAccountAsync(JsonObject record, CancellationToken cancellationToken)
    {
        logger.LogDebug("Before Request - Update single account");
        var result = await salesforceClient.UpdateAsync("Account", new List<JsonObject> { record }, cancellationToken);
        logger.LogDebug("After Request - Update single account");
        return result;
    }
}
