using System.Text.Json;

namespace hello.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation(JsonSerializer.Serialize(new
        {
            eventName = "route_entry",
            method = context.Request.Method,
            path = context.Request.Path.Value,
            resource = context.Request.Path.Value?.Trim('/').Split('/').FirstOrDefault() ?? "unknown"
        }));

        await _next(context);
    }
}
