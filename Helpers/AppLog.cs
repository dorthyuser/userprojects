using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Azfcsharp8Function;

/// <summary>
/// The Functions log (Application Insights, log stream) for every class. RequestLogMiddleware connects it to
/// each invocation and logs each answered request. Messages are plain text: never request or response bodies,
/// field values, secrets, tokens or connection strings.
/// </summary>
public static class AppLog
{
    private static ILogger? _logger;

    public static void Use(ILogger logger) => _logger = logger;

    public static void Info(string message) => Write(LogLevel.Information, message, null);

    public static void Warn(string message) => Write(LogLevel.Warning, message, null);

    /// <summary>Error level; the exception's type, message and stack trace go to the log, never to a response.</summary>
    public static void Error(string message, Exception? exception = null) => Write(LogLevel.Error, message, exception);

    /// <summary>One line per answered request: method, path (no query string), status, duration, correlation and invocation ids.</summary>
    public static void Request(HttpRequest request, int statusCode, TimeSpan elapsed, string invocationId)
    {
        var level = statusCode >= 500 ? LogLevel.Error : statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
        var correlationId = request.Headers.TryGetValue("X-Correlation-ID", out var id) && !string.IsNullOrWhiteSpace(id) ? id.ToString() : "-";
        Write(level, $"{request.Method} {request.Path} answered {statusCode} in {elapsed.TotalMilliseconds:0} ms correlationId={correlationId} invocationId={invocationId}", null);
    }

    private static void Write(LogLevel level, string message, Exception? exception)
    {
        if (_logger is null)
        {
            var text = exception is null ? message : $"{message}{Environment.NewLine}{exception}";
            if (level >= LogLevel.Error)
            {
                Console.Error.WriteLine(text);
            }
            else
            {
                Console.WriteLine(text);
            }
        }
        else
        {
            _logger.Log(level, exception, "{Message}", message);
        }
    }
}
