using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Azfcsharp8Function;

public sealed class AccountsFunctions
{
    private static readonly AccountsService Service = new();

    [Function("GetAccounts")]
    public async Task<IActionResult> GetAccounts([HttpTrigger(AuthorizationLevel.Function, "get", Route = "accounts")] HttpRequest req)
    {
        var result = await Service.GetAccountsAsync(req.Query["email"].ToString(), req.HttpContext.RequestAborted);
        return ToResult(req, result);
    }

    [Function("GetAccountById")]
    public async Task<IActionResult> GetAccountById([HttpTrigger(AuthorizationLevel.Function, "get", Route = "accounts/{id}")] HttpRequest req, string id)
    {
        var result = await Service.GetAccountByIdAsync(id, req.HttpContext.RequestAborted);
        return ToResult(req, result);
    }

    [Function("CreateAccount")]
    public async Task<IActionResult> CreateAccount([HttpTrigger(AuthorizationLevel.Function, "post", Route = "accounts")] HttpRequest req)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        if (!IsJsonContentType(req.ContentType))
        {
            return ToResult(req, Service.ApiKitUnsupportedMediaType());
        }

        if (!IsValidJson(body))
        {
            return ToResult(req, Service.ApiKitBadRequest());
        }

        var result = await Service.CreateAccountAsync(body, req.HttpContext.RequestAborted);
        return ToResult(req, result);
    }

    [Function("UpdateAccount")]
    public async Task<IActionResult> UpdateAccount([HttpTrigger(AuthorizationLevel.Function, "put", Route = "accounts/{id}")] HttpRequest req, string id)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        if (!IsJsonContentType(req.ContentType))
        {
            return ToResult(req, Service.ApiKitUnsupportedMediaType());
        }

        if (!IsValidJson(body))
        {
            return ToResult(req, Service.ApiKitBadRequest());
        }

        var result = await Service.UpdateAccountAsync(id, body, req.HttpContext.RequestAborted);
        return ToResult(req, result);
    }

    private static bool IsJsonContentType(string? contentType)
        => !string.IsNullOrWhiteSpace(contentType) && contentType.Split(';', 2)[0].Trim().Equals("application/json", System.StringComparison.OrdinalIgnoreCase);

    private static bool IsValidJson(string body)
    {
        try
        {
            using var _ = JsonDocument.Parse(body);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IActionResult ToResult(HttpRequest req, ApiResult result)
    {
        foreach (var header in result.Headers)
        {
            req.HttpContext.Response.Headers[header.Key] = header.Value;
        }
        if (result.StatusCode is 204 or 304)
        {
            return new StatusCodeResult(result.StatusCode);
        }
        return new ContentResult { StatusCode = result.StatusCode, Content = result.Body ?? string.Empty, ContentType = "application/json" };
    }
}
