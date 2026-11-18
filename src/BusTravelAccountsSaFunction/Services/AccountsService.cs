using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace BusTravelAccountsSaFunction;

public sealed class AccountsService(IAccountsRepository repository, ILogger<AccountsService> logger) : IAccountsService
{
    public async Task<IActionResult> GetAccountsAsync(string? email, CancellationToken cancellationToken)
    {
        var rows = await repository.GetByEmailAsync(email, cancellationToken);
        if (rows.Count == 0)
        {
            logger.LogWarning("GET /accounts answered 204 because no account matched the query");
            return new StatusCodeResult(204);
        }

        var response = rows.Select(Map).ToList();
        return new OkObjectResult(response);
    }

    public async Task<IActionResult> GetAccountByIdAsync(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var parsedId))
        {
            logger.LogWarning("GET /accounts/{Id} answered 204 because the id was invalid", id);
            return new StatusCodeResult(204);
        }

        var row = await repository.GetByIdAsync(parsedId, cancellationToken);
        if (row is null)
        {
            logger.LogWarning("GET /accounts/{Id} answered 204 because no account was found", id);
            return new StatusCodeResult(204);
        }

        return new OkObjectResult(Map(row));
    }

    public async Task<IActionResult> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var id = await repository.CreateAsync(request, cancellationToken);
            return new OkObjectResult(new AccountIdResponse { Id = id.ToString() });
        }
        catch (PostgresException ex)
        {
            logger.LogError(ex, "INSERT public.sa_accounts failed");
            logger.LogError("S3 replacement error log entry for failed create account");
            return ErrorResults.DatabaseFailure(ex.SqlState, "Failed to create the record");
        }
    }

    public async Task<IActionResult> UpdateAccountAsync(string id, UpdateAccountRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var parsedId))
        {
            logger.LogWarning("PUT /accounts/{Id} answered 204 because the id was invalid", id);
            return new StatusCodeResult(204);
        }

        try
        {
            var updatedId = await repository.UpdateAsync(parsedId, request, cancellationToken);
            return new OkObjectResult(new AccountIdResponse { Id = updatedId.ToString() });
        }
        catch (PostgresException ex)
        {
            logger.LogError(ex, "UPDATE public.sa_accounts failed");
            logger.LogError("S3 replacement error log entry for failed update account");
            return ErrorResults.DatabaseFailure(ex.SqlState, "Failed to update the record");
        }
    }

    private static AccountResponse Map(AccountRecord row)
    {
        return new AccountResponse
        {
            Id = row.Id.ToString().Trim(),
            Salutation = row.Salutation?.Trim(),
            FirstName = row.FirstName?.Trim(),
            LastName = row.LastName?.Trim(),
            PersonBirthdate = row.PersonBirthdate,
            Phone = row.Phone?.Trim(),
            MobilePhone = row.MobilePhone?.Trim(),
            PersonEmail = row.PersonEmail?.Trim(),
            MailingStreet = row.MailingStreet?.Trim(),
            MailingPostalCode = row.MailingPostalCode?.Trim(),
            MailingCity = row.MailingCity?.Trim(),
            MailingCountry = row.MailingCountry?.Trim(),
            AccountStatus = row.AccountStatus?.Trim(),
            AccountSource = row.AccountSource?.Trim() ?? string.Empty,
            Hotlisted = row.Hotlisted
        };
    }
}