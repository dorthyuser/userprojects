using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BusTravelAccountsSaFunction;

public sealed class AccountsFunctions(IAccountsService service, IErrorAuditClient errorAuditClient, ILogger<AccountsFunctions> logger)
{
    [Function("GetAccounts")]
    public async Task<IActionResult> GetAccounts(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "accounts")] HttpRequest req)
    {
        try
        {
            var result = await service.GetAccountsAsync(req.Query["email"].ToString(), req.HttpContext.RequestAborted);
            if (result.Count == 0)
            {
                return new StatusCodeResult(204);
            }

            return new OkObjectResult(result);
        }
        catch (SalesforceException ex) when (ex.Identifier == "CONNECTIVITY" && ex.Description.Contains("Invalid", StringComparison.Ordinal))
        {
            logger.LogError(ex, "Salesforce connectivity error");
            var response = ErrorResponse.Create(401, "SALESFORCE CONNECTIVITY ERROR", ex.Description);
            await errorAuditClient.WriteErrorAsync(response, req.Method, req.Path, req.HttpContext.RequestAborted);
            return new ObjectResult(response) { StatusCode = 401 };
        }
        catch (SalesforceException ex) when (ex.Identifier == "CONNECTIVITY" && ex.Description.Contains("Failed to send request", StringComparison.Ordinal))
        {
            logger.LogError(ex, "Salesforce connectivity error");
            var response = ErrorResponse.Create(404, "SALESFORCE CONNECTIVITY ERROR", ex.Description);
            await errorAuditClient.WriteErrorAsync(response, req.Method, req.Path, req.HttpContext.RequestAborted);
            return new ObjectResult(response) { StatusCode = 404 };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error");
            var response = ErrorResponse.Create(500, "MULE UNKNOWN ERROR", "An unexpected error occurred");
            await errorAuditClient.WriteErrorAsync(response, req.Method, req.Path, req.HttpContext.RequestAborted);
            throw;
        }
    }

    [Function("GetAccountById")]
    public async Task<IActionResult> GetAccountById(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "accounts/{id}")] HttpRequest req,
        string id)
    {
        try
        {
            var result = await service.GetAccountByIdAsync(id, req.HttpContext.RequestAborted);
            if (result is null)
            {
                return new StatusCodeResult(204);
            }

            return new OkObjectResult(result);
        }
        catch (SalesforceException ex) when (ex.Identifier == "CONNECTIVITY" && ex.Description.Contains("Invalid", StringComparison.Ordinal))
        {
            logger.LogError(ex, "Salesforce connectivity error");
            var response = ErrorResponse.Create(401, "SALESFORCE CONNECTIVITY ERROR", ex.Description);
            await errorAuditClient.WriteErrorAsync(response, req.Method, req.Path, req.HttpContext.RequestAborted);
            return new ObjectResult(response) { StatusCode = 401 };
        }
        catch (SalesforceException ex) when (ex.Identifier == "CONNECTIVITY" && ex.Description.Contains("Failed to send request", StringComparison.Ordinal))
        {
            logger.LogError(ex, "Salesforce connectivity error");
            var response = ErrorResponse.Create(404, "SALESFORCE CONNECTIVITY ERROR", ex.Description);
            await errorAuditClient.WriteErrorAsync(response, req.Method, req.Path, req.HttpContext.RequestAborted);
            return new ObjectResult(response) { StatusCode = 404 };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error");
            var response = ErrorResponse.Create(500, "MULE UNKNOWN ERROR", "An unexpected error occurred");
            await errorAuditClient.WriteErrorAsync(response, req.Method, req.Path, req.HttpContext.RequestAborted);
            throw;
        }
    }

    [Function("PostAccounts")]
    public async Task<IActionResult> PostAccounts(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "accounts")] HttpRequest req)
    {
        if (!RequestContentTypeHelper.IsJson(req.ContentType))
        {
            logger.LogWarning("Unsupported media type for POST /accounts");
            var response = ErrorResponse.Create(415, "UNSUPPORTED MEDIA TYPE", "Unsupported media type");
            await errorAuditClient.WriteErrorAsync(response, req.Method, req.Path, req.HttpContext.RequestAborted);
            return new ObjectResult(response) { StatusCode = 415 };
        }

        try
        {
            var request = await JsonSerializer.DeserializeAsync<AccountUpsertRequest>(req.Body, JsonDefaults.Options, req.HttpContext.RequestAborted);
            var result = await service.CreateAccountAsync(request ?? new AccountUpsertRequest(), req.HttpContext.RequestAborted);
            return new OkObjectResult(result);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Invalid JSON body");
            var response = ErrorResponse.Create(400, "BAD REQUEST", "Bad request");
            await errorAuditClient.WriteErrorAsync(response, req.Method, req.Path, req.HttpContext.RequestAborted);
            return new ObjectResult(response) { StatusCode = 400 };
        }
        catch (SalesforceOperationFailedException ex)
        {
            logger.LogError(ex, "Salesforce create failed");
            var response = ErrorResponse.Create(400, $"{ex.StatusCodeText} ERROR", ex.Description);
            await errorAuditClient.WriteErrorAsync(response, req.Method, req.Path, req.HttpContext.RequestAborted);
            return new ObjectResult(response) { StatusCode = 400 };
        }
        catch (SalesforceException ex) when (ex.Identifier == "CONNECTIVITY" && ex.Description.Contains("Invalid", StringComparison.Ordinal))
        {
            logger.LogError(ex, "Salesforce connectivity error");
            var response = ErrorResponse.Create(401, "SALESFORCE CONNECTIVITY ERROR", ex.Description);
            await errorAuditClient.WriteErrorAsync(response, req.Method, req.Path, req.HttpContext.RequestAborted);
            return new ObjectResult(response) { StatusCode = 401 };
        }
        catch (SalesforceException ex) when (ex.Identifier == "CONNECTIVITY" && ex.Description.Contains("Failed to send request", StringComparison.Ordinal))
        {
            logger.LogError(ex, "Salesforce connectivity error");
            var response = ErrorResponse.Create(404, "SALESFORCE CONNECTIVITY ERROR", ex.Description);
            await errorAuditClient.WriteErrorAsync(response, req.Method, req.Path, req.HttpContext.RequestAborted);
            return new ObjectResult(response) { StatusCode = 404 };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error");
            var response = ErrorResponse.Create(500, "MULE UNKNOWN ERROR", "An unexpected error occurred");
            await errorAuditClient.WriteErrorAsync(response, req.Method, req.Path, req.HttpContext.RequestAborted);
            throw;
        }
    }

    [Function("PutAccounts")]
    public async Task<IActionResult> PutAccounts(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "accounts/{id}")] HttpRequest req,
        string id)
    {
        if (!RequestContentTypeHelper.IsJson(req.ContentType))
        {
            logger.LogWarning("Unsupported media type for PUT /accounts/{id}");
            var response = ErrorResponse.Create(415, "UNSUPPORTED MEDIA TYPE", "Unsupported media type");
            await errorAuditClient.WriteErrorAsync(response, req.Method, req.Path, req.HttpContext.RequestAborted);
            return new ObjectResult(response) { StatusCode = 415 };
        }

        try
        {
            var request = await JsonSerializer.DeserializeAsync<AccountUpsertRequest>(req.Body, JsonDefaults.Options, req.HttpContext.RequestAborted);
            var result = await service.UpdateAccountAsync(id, request ?? new AccountUpsertRequest(), req.HttpContext.RequestAborted);
            return new OkObjectResult(result);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Invalid JSON body");
            var response = ErrorResponse.Create(400, "BAD REQUEST", "Bad request");
            await errorAuditClient.WriteErrorAsync(response, req.Method, req.Path, req.HttpContext.RequestAborted);
            return new ObjectResult(response) { StatusCode = 400 };
        }
        catch (SalesforceOperationFailedException ex)
        {
            logger.LogError(ex, "Salesforce update failed");
            var response = ErrorResponse.Create(400, $"{ex.StatusCodeText} ERROR", ex.Description);
            await errorAuditClient.WriteErrorAsync(response, req.Method, req.Path, req.HttpContext.RequestAborted);
            return new ObjectResult(response) { StatusCode = 400 };
        }
        catch (SalesforceException ex) when (ex.Identifier == "CONNECTIVITY" && ex.Description.Contains("Invalid", StringComparison.Ordinal))
        {
            logger.LogError(ex, "Salesforce connectivity error");
            var response = ErrorResponse.Create(401, "SALESFORCE CONNECTIVITY ERROR", ex.Description);
            await errorAuditClient.WriteErrorAsync(response, req.Method, req.Path, req.HttpContext.RequestAborted);
            return new ObjectResult(response) { StatusCode = 401 };
        }
        catch (SalesforceException ex) when (ex.Identifier == "CONNECTIVITY" && ex.Description.Contains("Failed to send request", StringComparison.Ordinal))
        {
            logger.LogError(ex, "Salesforce connectivity error");
            var response = ErrorResponse.Create(404, "SALESFORCE CONNECTIVITY ERROR", ex.Description);
            await errorAuditClient.WriteErrorAsync(response, req.Method, req.Path, req.HttpContext.RequestAborted);
            return new ObjectResult(response) { StatusCode = 404 };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error");
            var response = ErrorResponse.Create(500, "MULE UNKNOWN ERROR", "An unexpected error occurred");
            await errorAuditClient.WriteErrorAsync(response, req.Method, req.Path, req.HttpContext.RequestAborted);
            throw;
        }
    }
}
