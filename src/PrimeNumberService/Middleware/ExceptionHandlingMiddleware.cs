using System.Net;
using System.Text.Json;
using Npgsql;

namespace PrimeNumberService.Middleware;

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
        catch (PostgresException)
        {
            _logger.LogError("Database error occurred");
            await WriteProblemAsync(context, HttpStatusCode.ServiceUnavailable, "Database Error");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Operational error: {Message}", ex.Message);
            await WriteProblemAsync(context, HttpStatusCode.ServiceUnavailable, ex.Message);
        }
        catch (Exception)
        {
            _logger.LogError("Unhandled exception");
            await WriteProblemAsync(context, HttpStatusCode.InternalServerError, "Internal Error");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, HttpStatusCode statusCode, string detail)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        var payload = JsonSerializer.Serialize(new { detail });
        await context.Response.WriteAsync(payload);
    }
}