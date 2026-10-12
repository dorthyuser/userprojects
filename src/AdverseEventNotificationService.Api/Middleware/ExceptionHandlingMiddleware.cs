using System.Text.Json;
using AdverseEventNotificationService.Api.Exceptions;

namespace AdverseEventNotificationService.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException ex)
        {
            _logger.LogWarning("Handled API error {Code}", ex.Code);
            await WriteProblemAsync(context, ex.StatusCode, ex.Code);
        }
        catch (Exception)
        {
            _logger.LogError("Unhandled exception");
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "INTERNAL_SERVER_ERROR");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string code)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var payload = JsonSerializer.Serialize(new { error = code });
        await context.Response.WriteAsync(payload);
    }
}