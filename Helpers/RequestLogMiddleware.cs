using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace Azfcsharp8Function;

/// <summary>
/// Registered in Program.cs for every function: connects AppLog to the invocation and logs each answered HTTP
/// request once (method, path, status, duration, correlation and invocation ids). An exception that escapes a
/// function is logged with its stack trace and passed on (the host answers 500). Never write that line yourself.
/// </summary>
public sealed class RequestLogMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        AppLog.Use(context.GetLogger(context.FunctionDefinition.Name));
        var http = context.GetHttpContext();
        if (http is null)
        {
            await next(context);
            return;
        }

        var started = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
            // The ASP.NET Core integration writes the function's result after this middleware: its status is the answer
            var status = (context.GetInvocationResult().Value as IStatusCodeActionResult)?.StatusCode ?? http.Response.StatusCode;
            AppLog.Request(http.Request, status, Stopwatch.GetElapsedTime(started), context.InvocationId);
        }
        catch (Exception ex)
        {
            AppLog.Error($"{http.Request.Method} {http.Request.Path} failed", ex);
            throw;
        }
    }
}
