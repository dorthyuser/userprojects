using System.Globalization;
using System.Text.Json.Nodes;

namespace BusTravelAccountsSaFunction;

public sealed class AccountsService(IAccountsRepository repository) : IAccountsService
{
    public async Task<IReadOnlyList<AccountResponse>> GetAccountsAsync(string? email, CancellationToken cancellationToken)
    {
        var enrichedEmail = (email ?? string.Empty).Replace("'", "\\'", StringComparison.Ordinal);
        var records = await repository.GetAccountsByEmailAsync(enrichedEmail, cancellationToken);
        return records.Select(MapAccount).ToList();
    }

    public async Task<AccountResponse?> GetAccountByIdAsync(string id, CancellationToken cancellationToken)
    {
        var records = await repository.GetAccountByIdAsync(id, cancellationToken);
        return records.Count > 0 ? MapAccount(records[0]) : null;
    }

    public async Task<AccountIdResponse> CreateAccountAsync(AccountUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await repository.CreateAccountAsync(BuildCreateOrUpdateRecord(null, request), cancellationToken);
        return MapBulkResult(result);
    }

    public async Task<AccountIdResponse> UpdateAccountAsync(string id, AccountUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await repository.UpdateAccountAsync(BuildCreateOrUpdateRecord(id, request), cancellationToken);
        return MapBulkResult(result);
    }

    private static JsonObject BuildCreateOrUpdateRecord(string? id, AccountUpsertRequest request)
    {
        var record = new JsonObject();

        if (id is not null)
        {
            record["Id"] = Trim(id);
        }

        record["LastName"] = Trim(request.LastName);
        record["FirstName"] = Trim(request.FirstName);
        record["Salutation"] = id is null ? Trim(request.Salutation) : request.Salutation;
        record["PersonMailingStreet"] = Trim(request.MailingStreet);
        record["PersonMailingCity"] = Trim(request.MailingCity);
        record["PersonMailingPostalCode"] = Trim(request.MailingPostalCode);
        record["PersonMailingCountry"] = Trim(request.MailingCountry);
        record["PersonEmail"] = Trim(request.PersonEmail);
        record["Account_Status__c"] = Trim(request.AccountStatus);
        record["Source__c"] = Trim(request.AccountSource);

        if (request.Phone is not null)
        {
            record["Phone"] = Trim(request.Phone);
        }

        if (request.MobilePhone is not null)
        {
            record["PersonMobilePhone"] = Trim(request.MobilePhone);
        }

        if (request.PersonBirthdate is not null)
        {
            var date = DateOnly.Parse(request.PersonBirthdate, CultureInfo.InvariantCulture);
            record["PersonBirthdate"] = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return record;
    }

    private static AccountResponse MapAccount(JsonObject record)
    {
        var mailingAddress = record["PersonMailingAddress"] as JsonObject;
        return new AccountResponse
        {
            Id = Trim(GetString(record, "Id")),
            Salutation = Trim(GetString(record, "Salutation")),
            FirstName = Trim(GetString(record, "FirstName")),
            LastName = Trim(GetString(record, "LastName")),
            PersonBirthdate = GetString(record, "PersonBirthdate"),
            Phone = Trim(GetString(record, "Phone")),
            MobilePhone = Trim(GetString(record, "PersonMobilePhone")),
            PersonEmail = Trim(GetString(record, "PersonEmail")),
            MailingStreet = Trim(GetString(mailingAddress, "street")),
            MailingPostalCode = Trim(GetString(mailingAddress, "postalCode")),
            MailingCity = Trim(GetString(mailingAddress, "city")),
            MailingCountry = Trim(GetString(mailingAddress, "country")),
            AccountStatus = Trim(GetString(record, "Account_Status__c")),
            AccountSource = Trim(GetString(record, "Source__c")) ?? string.Empty,
            Hotlisted = GetBoolean(record, "Hotlisted__c")
        };
    }

    private static AccountIdResponse MapBulkResult(SalesforceBulkResult result)
    {
        if (!result.Successful)
        {
            var first = result.Items[0].Payload["errors"]?.AsArray()[0]?.AsObject();
            var statusCode = first?["statusCode"]?.GetValue<string>() ?? string.Empty;
            var description = first?["message"]?.GetValue<string>() ?? string.Empty;
            throw new SalesforceOperationFailedException(statusCode, description);
        }

        var idNode = result.Items[0].Payload["id"];
        string? id = null;
        if (idNode is JsonArray array && array.Count > 0)
        {
            id = array[0]?.GetValue<string>();
        }
        else if (idNode is not null)
        {
            id = idNode.GetValue<string>();
        }

        return new AccountIdResponse { Id = id };
    }

    private static string? GetString(JsonObject? obj, string name) => obj is not null && obj[name] is not null ? obj[name]!.GetValue<string>() : null;

    private static bool? GetBoolean(JsonObject obj, string name) => obj[name] is not null ? obj[name]!.GetValue<bool>() : null;

    private static string? Trim(string? value) => value?.Trim();
}
