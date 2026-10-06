using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BusTravelAccountsEaMainFunction;

public sealed class AccountsFunctions
{
    private readonly ILogger<AccountsFunctions> _logger;
    private readonly Settings _settings;
    private readonly AccountsService _service;
    private static readonly JsonSerializerOptions JsonOptions = JsonOptionsHelper.Create();

    public AccountsFunctions(ILogger<AccountsFunctions> logger)
    {
        _logger = logger;
        _settings = new Settings();
        _service = new AccountsService(_settings);
    }

    [Function("GetAccounts")]
    public async Task<IActionResult> GetAccounts([HttpTrigger(AuthorizationLevel.Function, "get", Route = "accounts")] HttpRequest req)
    {
        return await ExecuteAsync(req, async correlationId => await _service.GetAccountsAsync(req.Query, correlationId, req.HttpContext.RequestAborted).ConfigureAwait(false)).ConfigureAwait(false);
    }

    [Function("GetAccountById")]
    public async Task<IActionResult> GetAccountById([HttpTrigger(AuthorizationLevel.Function, "get", Route = "accounts/{id}")] HttpRequest req, string id)
    {
        return await ExecuteAsync(req, async correlationId => await _service.GetAccountByIdAsync(id, correlationId, req.HttpContext.RequestAborted).ConfigureAwait(false)).ConfigureAwait(false);
    }

    [Function("CreateAccount")]
    public async Task<IActionResult> CreateAccount([HttpTrigger(AuthorizationLevel.Function, "post", Route = "accounts")] HttpRequest req)
    {
        if (!IsJson(req.ContentType))
        {
            return BuildError(415, "UNSUPPORTED MEDIA TYPE", null);
        }

        var body = await new StreamReader(req.Body).ReadToEndAsync().ConfigureAwait(false);
        return await ExecuteAsync(req, async correlationId => await _service.CreateAccountAsync(body, correlationId, req.HttpContext.RequestAborted).ConfigureAwait(false)).ConfigureAwait(false);
    }

    [Function("UpdateAccount")]
    public async Task<IActionResult> UpdateAccount([HttpTrigger(AuthorizationLevel.Function, "put", Route = "accounts/{id}")] HttpRequest req, string id)
    {
        if (!IsJson(req.ContentType))
        {
            return BuildError(415, "UNSUPPORTED MEDIA TYPE", null);
        }

        var body = await new StreamReader(req.Body).ReadToEndAsync().ConfigureAwait(false);
        return await ExecuteAsync(req, async correlationId => await _service.UpdateAccountAsync(id, body, correlationId, req.HttpContext.RequestAborted).ConfigureAwait(false)).ConfigureAwait(false);
    }

    [Function("Alive")]
    public IActionResult Alive([HttpTrigger(AuthorizationLevel.Function, "get", Route = "alive")] HttpRequest req)
    {
        return new ContentResult { StatusCode = 200, Content = "UP", ContentType = "text/plain" };
    }

    [Function("Ready")]
    public IActionResult Ready([HttpTrigger(AuthorizationLevel.Function, "get", Route = "ready")] HttpRequest req)
    {
        return new ContentResult { StatusCode = 200, Content = "UP", ContentType = "text/plain" };
    }

    private async Task<IActionResult> ExecuteAsync(HttpRequest req, Func<string, Task<DownstreamResponse>> action)
    {
        try
        {
            var correlationId = CorrelationIdHelper.GetCorrelationId(req.Headers);
            var response = await action(correlationId).ConfigureAwait(false);
            req.HttpContext.Response.Headers["X-Correlation-ID"] = correlationId;
            return new ContentResult { StatusCode = response.StatusCode, Content = response.Body, ContentType = response.ContentType ?? "application/json" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Type} {Message}", ex.GetType().FullName, ex.Message);
            return BuildError(500, "INTERNAL SERVER ERROR", "Unexpected error");
        }
    }

    private IActionResult BuildError(int statusCode, string message, object? description)
    {
        var payload = new ErrorResponse
        {
            Error = new ErrorDetails
            {
                ErrorCode = statusCode,
                ErrorDateTime = DateTimeOffset.UtcNow,
                ErrorMessage = message,
                ErrorDescription = description
            }
        };

        return new ContentResult
        {
            StatusCode = statusCode,
            Content = JsonSerializer.Serialize(payload, JsonOptions),
            ContentType = "application/json"
        };
    }

    private static bool IsJson(string? contentType)
    {
        return string.IsNullOrWhiteSpace(contentType) || contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);
    }
}