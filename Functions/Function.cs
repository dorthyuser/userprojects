using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace BusTravelAccountsLambda;

public sealed class Function
{
    private static readonly EaAccountsService Ea = new();
    private static readonly EaSettings Settings = new();

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
        var path = request.Path ?? "/";
        var basePath = Settings.ListenerPath;
        var stripped = RequestHelper.StripBasePath(path, basePath);
        var headers = request.Headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var query = request.QueryStringParameters;
        var body = RequestHelper.GetBody(request);
        var correlationId = RequestHelper.GetCorrelationId(headers);
        var customerCorrelationId = RequestHelper.GetCustomerCorrelationId(headers);
        var internalHeaders = RequestHelper.Headers(correlationId, customerCorrelationId);

        if (IsAlive(stripped))
        {
            var result = await Ea.AliveAsync();
            return ToResponse(result);
        }

        if (IsReady(stripped))
        {
            var result = await Ea.ReadyAsync();
            return ToResponse(result);
        }

        if (request.HttpMethod is null)
        {
            return Error(404, "RESOURCE NOT FOUND", "An unexpected error occurred");
        }

        var method = request.HttpMethod.ToUpperInvariant();
        if (method == "GET" && stripped == "/accounts")
        {
            var result = await Ea.GetAccountsAsync(query, internalHeaders);
            return ToResponse(result);
        }

        if (method == "GET" && stripped.StartsWith("/accounts/", StringComparison.OrdinalIgnoreCase))
        {
            var id = stripped[("/accounts/".Length)..];
            var result = await Ea.GetAccountByIdAsync(id, query, internalHeaders);
            return ToResponse(result);
        }

        if (method == "POST" && stripped == "/accounts")
        {
            if (!IsJson(request.Headers))
            {
                return Error(415, "UNSUPPORTED MEDIA TYPE", "Unsupported Media Type");
            }
            var result = await Ea.PostAccountsAsync(body, query, internalHeaders);
            return ToResponse(result);
        }

        if (method == "PUT" && stripped.StartsWith("/accounts/", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsJson(request.Headers))
            {
                return Error(415, "UNSUPPORTED MEDIA TYPE", "Unsupported Media Type");
            }
            var id = stripped[("/accounts/".Length)..];
            var result = await Ea.PutAccountByIdAsync(id, body, query, internalHeaders);
            return ToResponse(result);
        }

        if (stripped.StartsWith("/accounts", StringComparison.OrdinalIgnoreCase))
        {
            return Error(405, "METHOD NOT ALLOWED", "Method Not Allowed");
        }

        return Error(404, "RESOURCE NOT FOUND", "Resource Not Found");
    }

    private static bool IsAlive(string path) => string.Equals(path, "/alive", StringComparison.OrdinalIgnoreCase);
    private static bool IsReady(string path) => string.Equals(path, "/ready", StringComparison.OrdinalIgnoreCase);

    private static bool IsJson(IDictionary<string, string>? headers)
    {
        var contentType = RequestHelper.GetHeader(headers, "Content-Type");
        return string.IsNullOrWhiteSpace(contentType) || contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }

    private static APIGatewayProxyResponse ToResponse(EaApiResult result)
    {
        var headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase);
        if (!headers.ContainsKey("Content-Type"))
        {
            headers["Content-Type"] = string.IsNullOrWhiteSpace(result.Body) || result.Body == "UP" || result.Body == "DOWN" ? "text/plain" : "application/json";
        }

        return new APIGatewayProxyResponse
        {
            StatusCode = result.StatusCode,
            Headers = headers,
            Body = result.Body
        };
    }

    private static APIGatewayProxyResponse Error(int statusCode, string errorMessage, string description)
    {
        var body = JsonSerializer.Serialize(new EaErrorResponse
        {
            Error = new EaErrorDetails
            {
                ErrorCode = statusCode,
                ErrorDateTime = DateTime.UtcNow,
                ErrorMessage = errorMessage,
                ErrorDescription = description
            }
        }, JsonHelper.Options);

        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Headers = ResponseHelper.JsonHeaders(),
            Body = body
        };
    }
}