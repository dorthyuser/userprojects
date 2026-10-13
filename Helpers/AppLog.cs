using LambdaLogLevel = Amazon.Lambda.Core.LogLevel;

namespace BusTravelAccountsSaLambda;

/// <summary>
/// The Lambda log (CloudWatch) for every class. Function.FunctionHandler connects it to the
/// invocation's logger and logs each answered request. Messages are plain text: never request or
/// response bodies, field values, secrets, tokens or connection strings.
/// </summary>
public static class AppLog
{
    private static ILambdaLogger? _logger;

    public static void Use(ILambdaLogger logger) => _logger = logger;

    public static void Info(string message) => Write(LambdaLogLevel.Information, message, null);

    public static void Warn(string message) => Write(LambdaLogLevel.Warning, message, null);

    /// <summary>Error level; the exception's type, message and stack trace go to the log, never to a response.</summary>
    public static void Error(string message, Exception? exception = null) => Write(LambdaLogLevel.Error, message, exception);

    /// <summary>One line per answered request: method, path (no query string), status, duration and request ids.</summary>
    public static void Request(APIGatewayProxyRequest request, int statusCode, TimeSpan elapsed)
    {
        var level = statusCode >= 500 ? LambdaLogLevel.Error : statusCode >= 400 ? LambdaLogLevel.Warning : LambdaLogLevel.Information;
        var correlationId = request.Headers != null && request.Headers.TryGetValue("X-Correlation-ID", out var id) && !string.IsNullOrWhiteSpace(id) ? id : "-";
        Write(level, $"{request.HttpMethod} {request.Path} answered {statusCode} in {elapsed.TotalMilliseconds:0} ms correlationId={correlationId} apiRequestId={request.RequestContext?.RequestId ?? "-"}", null);
    }

    private static void Write(LambdaLogLevel level, string message, Exception? exception)
    {
        if (_logger is null)
        {
            LambdaLogger.Log(exception is null ? message : $"{message}{Environment.NewLine}{exception}");
        }
        else if (exception is null)
        {
            _logger.Log(level, message);
        }
        else
        {
            _logger.Log(level, exception, message);
        }
    }
}
