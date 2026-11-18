using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BusTravelAccountsSaFunction;

public sealed class AccountsFunctions(IAccountsService service, ILogger<AccountsFunctions> logger)
{
    [Function("GetAccounts")]
    public async Task<IActionResult> GetAccounts(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "accounts")] HttpRequest req)
    {
        var result = await service.GetAccountsAsync(req.Query["email"].ToString(), req.HttpContext.RequestAborted);
        return result;
    }

    [Function("GetAccountById")]
    public async Task<IActionResult> GetAccountById(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "accounts/{id}")] HttpRequest req,
        string id)
    {
        var result = await service.GetAccountByIdAsync(id, req.HttpContext.RequestAborted);
        return result;
    }

    [Function("PostAccounts")]
    public async Task<IActionResult> PostAccounts(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "accounts")] HttpRequest req)
    {
        if (!ContentTypeHelper.IsJson(req.ContentType))
        {
            logger.LogWarning("POST /accounts answered 415 because the Content-Type is unsupported");
            return ErrorResults.UnsupportedMediaType();
        }

        CreateAccountRequest request;
        try
        {
            request = await RequestBodyHelper.ReadRequiredJsonAsync<CreateAccountRequest>(req, req.HttpContext.RequestAborted);
        }
        catch (EmptyBodyException ex)
        {
            logger.LogError(ex, "POST /accounts failed because the request body was empty");
            return ErrorResults.MuleUnknown("check payload existence or method/payload combination requirement");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "POST /accounts failed because the request body could not be parsed as JSON");
            logger.LogWarning("POST /accounts answered 400 because the request body is invalid JSON");
            return ErrorResults.BadRequest("BAD REQUEST");
        }

        var result = await service.CreateAccountAsync(request, req.HttpContext.RequestAborted);
        return result;
    }

    [Function("PutAccounts")]
    public async Task<IActionResult> PutAccounts(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "accounts/{id}")] HttpRequest req,
        string id)
    {
        if (!ContentTypeHelper.IsJson(req.ContentType))
        {
            logger.LogWarning("PUT /accounts/{Id} answered 415 because the Content-Type is unsupported", id);
            return ErrorResults.UnsupportedMediaType();
        }

        UpdateAccountRequest request;
        try
        {
            request = await RequestBodyHelper.ReadRequiredJsonAsync<UpdateAccountRequest>(req, req.HttpContext.RequestAborted);
        }
        catch (EmptyBodyException ex)
        {
            logger.LogError(ex, "PUT /accounts/{Id} failed because the request body was empty", id);
            return ErrorResults.MuleUnknown("check payload existence or method/payload combination requirement");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "PUT /accounts/{Id} failed because the request body could not be parsed as JSON", id);
            logger.LogWarning("PUT /accounts/{Id} answered 400 because the request body is invalid JSON", id);
            return ErrorResults.BadRequest("BAD REQUEST");
        }

        var result = await service.UpdateAccountAsync(id, request, req.HttpContext.RequestAborted);
        return result;
    }
}
