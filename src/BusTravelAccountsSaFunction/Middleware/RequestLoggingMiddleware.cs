using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace BusTravelAccountsSaFunction;

/// <summary>
/// Every HTTP invocation: the caller's X-Correlation-ID (or a new one) on the response and on every log line of the
/// invocation (a log scope), then one line per answered request — method, path, status, duration. Never bodies,
/// query strings or header values. Added by the build: never write or change it.
/// </summary>
public sealed class RequestLoggingMiddleware(ILogger<RequestLoggingMiddleware> logger) : IFunctionsWorkerMiddleware
{
    private const string CorrelationHeader = "X-Correlation-ID";

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var http = context.GetHttpContext();
        if (http is null)
        {
            await next(context);
            return;
        }

        var given = http.Request.Headers[CorrelationHeader].ToString();
        var correlationId = IsPlainToken(given) ? given : Guid.NewGuid().ToString();
        // Before the function answers: with ASP.NET Core integration a header set afterwards may come too late
        http.Response.Headers[CorrelationHeader] = correlationId;

        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["InvocationId"] = context.InvocationId
        });
        var started = Stopwatch.GetTimestamp();
        await next(context);
        // The ASP.NET Core integration writes the function's result after this middleware: its status is the answer
        var status = (context.GetInvocationResult().Value as IStatusCodeActionResult)?.StatusCode ?? http.Response.StatusCode;
        var level = status >= 500 ? LogLevel.Error : status >= 400 ? LogLevel.Warning : LogLevel.Information;
        logger.Log(level, "{Method} {Path} answered {Status} in {ElapsedMs} ms",
            http.Request.Method, http.Request.Path.Value, status, (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }

    // The caller's id is used only when it is a plain token: any other value could forge log lines or response headers
    private static bool IsPlainToken(string value) =>
        value.Length is > 0 and <= 64 && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or ':');
}
