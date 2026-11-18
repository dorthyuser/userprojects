using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BusTravelAccountsSaFunction;

public sealed class AccountsFunctions(IAccountsService service, ILogger<AccountsFunctions> logger)
{
    [Function("GetAccounts")]
    public async Task<IActionResult> GetAccounts(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "accounts")] HttpRequest req)
    {
        var result = await service.GetAccountsAsync(req.Query["email"].ToString(), req.HttpContext.RequestAborted);
        return result.ToActionResult();
    }

    [Function("GetAccountById")]
    public async Task<IActionResult> GetAccountById(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "accounts/{id}")] HttpRequest req,
        string id)
    {
        var result = await service.GetAccountByIdAsync(id, req.HttpContext.RequestAborted);
        return result.ToActionResult();
    }

    [Function("PostAccounts")]
    public async Task<IActionResult> PostAccounts(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "accounts")] HttpRequest req)
    {
        if (!MediaTypeHelper.IsJson(req.ContentType))
        {
            return ErrorResults.UnsupportedMediaType();
        }

        if (!RequestBodyHelper.HasBody(req))
        {
            logger.LogError("Empty request body for POST /accounts");
            return ErrorResults.MuleUnknownPayloadError();
        }

        CreateAccountRequest request;
        try
        {
            request = (await JsonSerializer.DeserializeAsync<CreateAccountRequest>(
                req.Body,
                JsonDefaults.Options,
                req.HttpContext.RequestAborted)) ?? throw new JsonException("Request body deserialized to null.");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Invalid JSON body for POST /accounts");
            return ErrorResults.BadRequest();
        }

        var result = await service.CreateAccountAsync(request, req.HttpContext.RequestAborted);
        return result.ToActionResult();
    }

    [Function("PutAccounts")]
    public async Task<IActionResult> PutAccounts(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "accounts/{id}")] HttpRequest req,
        string id)
    {
        if (!MediaTypeHelper.IsJson(req.ContentType))
        {
            return ErrorResults.UnsupportedMediaType();
        }

        if (!RequestBodyHelper.HasBody(req))
        {
            logger.LogError("Empty request body for PUT /accounts/{Id}", id);
            return ErrorResults.MuleUnknownPayloadError();
        }

        UpdateAccountRequest request;
        try
        {
            request = (await JsonSerializer.DeserializeAsync<UpdateAccountRequest>(
                req.Body,
                JsonDefaults.Options,
                req.HttpContext.RequestAborted)) ?? throw new JsonException("Request body deserialized to null.");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Invalid JSON body for PUT /accounts/{Id}", id);
            return ErrorResults.BadRequest();
        }

        var result = await service.UpdateAccountAsync(id, request, req.HttpContext.RequestAborted);
        return result.ToActionResult();
    }
}