using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace BusTravelAccountsSaFunction;

/// <summary>
/// An exception a function lets escape: logged with its stack trace, and answered as the Mule app answers an
/// unexpected error — ErrorResults.Unexpected(), the Mule error body, never the exception's text. Added by the build:
/// never write or change it.
/// </summary>
public sealed class ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger) : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (context.GetHttpContext() is not null)
        {
            logger.LogError(exception, "{Function} failed", context.FunctionDefinition.Name);
            context.GetInvocationResult().Value = ErrorResults.Unexpected();
        }
    }
}
