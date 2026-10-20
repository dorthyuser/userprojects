using System.Text;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace BusTravelAccountsLambda;

public sealed class Function
{
    private static readonly EaAccountsService AccountsService = new();

    /// <summary>
    /// Lambda entry point: makes the header names case-insensitive (clients and HTTP/2 send them in any case),
    /// connects AppLog to this invocation and logs every answered request once; HandleRequestAsync answers it.
    /// </summary>
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        request.Headers = new Dictionary<string, string>(request.Headers ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
        AppLog.Use(context.Logger);
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            var response = await HandleRequestAsync(request, context);
            AppLog.Request(request, response.StatusCode, System.Diagnostics.Stopwatch.GetElapsedTime(started));
            return response;
        }
        catch (Exception ex)
        {
            AppLog.Error($"{request.HttpMethod} {request.Path} failed", ex);
            throw;
        }
    }

    private async Task<APIGatewayProxyResponse> HandleRequestAsync(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            var settings = EaSettings.Instance;
            var basePath = settings.ListenerPath;
            var path = request.Path ?? string.Empty;
            var strippedPath = BasePath.Strip(path, basePath);
            var method = request.HttpMethod?.ToUpperInvariant() ?? string.Empty;
            var headers = request.Headers;
            var query = request.QueryStringParameters;
            var body = request.IsBase64Encoded && !string.IsNullOrEmpty(request.Body)
                ? Encoding.UTF8.GetString(Convert.FromBase64String(request.Body))
                : request.Body;

            if (IsAlive(strippedPath))
            {
                if (!IsAnyMethod(method))
                {
                    return BuildError(405, "METHOD NOT ALLOWED");
                }

                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                    Headers = new Dictionary<string, string> { ["Content-Type"] = "text/plain" },
                    Body = "UP"
                };
            }

            if (IsReady(strippedPath))
            {
                if (!IsAnyMethod(method))
                {
                    return BuildError(405, "METHOD NOT ALLOWED");
                }

                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                    Headers = new Dictionary<string, string> { ["Content-Type"] = "text/plain" },
                    Body = "UP"
                };
            }

            if (string.IsNullOrEmpty(strippedPath) || strippedPath == "/")
            {
                return BuildError(404, "RESOURCE NOT FOUND");
            }

            if (method == "GET" && strippedPath == "/accounts")
            {
                var result = await AccountsService.GetAccountsAsync(query, headers, body);
                return ToResponse(result);
            }

            if (method == "GET" && TryGetAccountId(strippedPath, out var id))
            {
                var result = await AccountsService.GetAccountByIdAsync(id, query, headers, body);
                return ToResponse(result);
            }

            if (method == "POST" && strippedPath == "/accounts")
            {
                if (!IsJsonBodyAllowed(headers))
                {
                    return BuildError(415, "UNSUPPORTED MEDIA TYPE");
                }

                if (!IsValidJsonBody(body))
                {
                    return BuildError(400, "BAD REQUEST");
                }

                var result = await AccountsService.PostAccountsAsync(query, headers, body);
                return ToResponse(result);
            }

            if (method == "PUT" && TryGetAccountId(strippedPath, out var putId))
            {
                if (!IsJsonBodyAllowed(headers))
                {
                    return BuildError(415, "UNSUPPORTED MEDIA TYPE");
                }

                if (!IsValidJsonBody(body))
                {
                    return BuildError(400, "BAD REQUEST");
                }

                var result = await AccountsService.PutAccountByIdAsync(putId, query, headers, body);
                return ToResponse(result);
            }

            if (IsKnownPath(strippedPath))
            {
                return BuildError(405, "METHOD NOT ALLOWED");
            }

            return BuildError(404, "RESOURCE NOT FOUND");
        }
        catch (Exception ex)
        {
            AppLog.Error("Unhandled error", ex);
            return BuildError(500, "MULE UNKNOWN ERROR");
        }
    }

    private static bool IsAnyMethod(string method) => !string.IsNullOrEmpty(method);

    private static bool IsAlive(string path) => path.Equals("/alive", StringComparison.OrdinalIgnoreCase);

    private static bool IsReady(string path) => path.Equals("/ready", StringComparison.OrdinalIgnoreCase);

    private static bool IsKnownPath(string path) => path.Equals("/accounts", StringComparison.OrdinalIgnoreCase) || TryGetAccountId(path, out _);

    private static bool TryGetAccountId(string path, out string id)
    {
        id = string.Empty;
        if (!path.StartsWith("/accounts/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        id = path["/accounts/".Length..];
        return !string.IsNullOrWhiteSpace(id) && !id.Contains('/');
    }

    private static bool IsJsonBodyAllowed(IDictionary<string, string>? headers)
    {
        if (headers is null)
        {
            return false;
        }

        var contentType = HeaderHelper.GetHeader(headers, "Content-Type");
        return contentType is null || contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidJsonBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        try
        {
            using var _ = System.Text.Json.JsonDocument.Parse(body);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static APIGatewayProxyResponse ToResponse(EaApiResult result)
    {
        var headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase);
        if (!headers.ContainsKey("Content-Type"))
        {
            headers["Content-Type"] = result.Body is null || result.Body.TrimStart().StartsWith("{") || result.Body.TrimStart().StartsWith("[") ? "application/json" : "text/plain";
        }

        return new APIGatewayProxyResponse
        {
            StatusCode = result.StatusCode,
            Headers = headers,
            Body = result.Body ?? string.Empty
        };
    }

    private static APIGatewayProxyResponse BuildError(int statusCode, string message)
    {
        var body = System.Text.Json.JsonSerializer.Serialize(new ErrorBody { Error = new ErrorDetail { ErrorCode = statusCode, ErrorDateTime = DateTimeOffset.UtcNow, ErrorMessage = message, ErrorDescription = message } }, JsonOptions.Default);
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            Body = body
        };
    }
}