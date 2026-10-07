using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MulecombineMainLambda;

public sealed class Function
{
    private static readonly EaApiService EaApiService = new();

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            var method = request.HttpMethod?.ToUpperInvariant() ?? string.Empty;
            var requestPath = request.Path ?? string.Empty;
            var basePath = EaSettings.Current.ListenerPath;
            var relativePath = PathHelper.StripBasePath(requestPath, basePath);
            var headers = request.Headers;
            var correlationId = CorrelationHelper.GetCorrelationId(headers);
            var customerCorrelationId = HeaderHelper.GetHeader(headers, "CUSTOMER_CORRELATION_ID")?.Trim() ?? "CUSTOMER_CORRELATION_ID_NOT_FOUND";
            var basicDetails = new Dictionary<string, string?>
            {
                ["CUSTOMER_CORRELATION_ID"] = customerCorrelationId,
                ["X_CORRELATION_ID"] = correlationId,
                ["client_id"] = HeaderHelper.GetHeader(headers, "client_id")?.Trim(),
                ["httpMethod"] = method,
                ["relativePath"] = relativePath
            };

            context.Logger.LogLine($"START - Request received {JsonSerializer.Serialize(new { basicDetails, payload = request.Body })}");

            if (PathHelper.IsAlivePath(relativePath))
            {
                if (method != "GET")
                {
                    return BuildErrorResponse(405, "METHOD NOT ALLOWED", "Method not allowed", correlationId);
                }

                var alive = await EaApiService.GetAliveAsync();
                return ToProxyResponse(alive, correlationId);
            }

            if (PathHelper.IsReadyPath(relativePath))
            {
                if (method != "GET")
                {
                    return BuildErrorResponse(405, "METHOD NOT ALLOWED", "Method not allowed", correlationId);
                }

                var ready = await EaApiService.GetReadyAsync();
                return ToProxyResponse(ready, correlationId);
            }

            if (relativePath.Equals("/accounts", StringComparison.OrdinalIgnoreCase))
            {
                if (method == "GET")
                {
                    var result = await EaApiService.GetAccountsAsync(request.QueryStringParameters, headers);
                    return ToProxyResponse(result, correlationId);
                }

                if (method == "POST")
                {
                    if (!IsJsonContentType(headers))
                    {
                        return BuildErrorResponse(415, "UNSUPPORTED MEDIA TYPE", "Unsupported media type", correlationId);
                    }

                    var body = DecodeBody(request);
                    var result = await EaApiService.PostAccountsAsync(body, headers);
                    return ToProxyResponse(result, correlationId);
                }

                return BuildErrorResponse(405, "METHOD NOT ALLOWED", "Method not allowed", correlationId);
            }

            if (relativePath.StartsWith("/accounts/", StringComparison.OrdinalIgnoreCase))
            {
                var id = relativePath[("/accounts/".Length)..];
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BuildErrorResponse(404, "RESOURCE NOT FOUND", "Resource not found", correlationId);
                }

                if (method == "GET")
                {
                    var result = await EaApiService.GetAccountByIdAsync(id, request.QueryStringParameters, headers);
                    return ToProxyResponse(result, correlationId);
                }

                if (method == "PUT")
                {
                    if (!IsJsonContentType(headers))
                    {
                        return BuildErrorResponse(415, "UNSUPPORTED MEDIA TYPE", "Unsupported media type", correlationId);
                    }

                    var body = DecodeBody(request);
                    var result = await EaApiService.PutAccountAsync(id, body, headers);
                    return ToProxyResponse(result, correlationId);
                }

                return BuildErrorResponse(405, "METHOD NOT ALLOWED", "Method not allowed", correlationId);
            }

            return BuildErrorResponse(404, "RESOURCE NOT FOUND", "Resource not found", correlationId);
        }
        catch (Exception exception)
        {
            context.Logger.LogLine($"Unhandled exception {exception.GetType().FullName}: {exception.Message}");
            var error = EaApiService.BuildUnexpectedError(exception);
            return ToProxyResponse(error, CorrelationHelper.GetCorrelationId(request.Headers));
        }
    }

    private static bool IsJsonContentType(IDictionary<string, string>? headers)
    {
        var contentType = HeaderHelper.GetHeader(headers, "Content-Type") ?? HeaderHelper.GetHeader(headers, "content-type");
        return !string.IsNullOrWhiteSpace(contentType) && contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }

    private static string? DecodeBody(APIGatewayProxyRequest request)
    {
        if (string.IsNullOrEmpty(request.Body))
        {
            return request.Body;
        }

        if (!request.IsBase64Encoded)
        {
            return request.Body;
        }

        var bytes = Convert.FromBase64String(request.Body);
        return Encoding.UTF8.GetString(bytes);
    }

    private static APIGatewayProxyResponse ToProxyResponse(EaApiResult result, string correlationId)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (result.Headers != null)
        {
            foreach (var header in result.Headers)
            {
                headers[header.Key] = header.Value;
            }
        }

        headers["X-Correlation-ID"] = correlationId;

        return new APIGatewayProxyResponse
        {
            StatusCode = result.StatusCode,
            Headers = headers,
            Body = result.Body
        };
    }

    private static APIGatewayProxyResponse BuildErrorResponse(int statusCode, string errorMessage, string errorDescription, string correlationId)
    {
        var body = JsonSerializer.Serialize(new EaErrorResponse
        {
            Error = new EaErrorBody
            {
                ErrorCode = statusCode,
                ErrorDateTime = DateTimeOffset.UtcNow,
                ErrorMessage = errorMessage,
                ErrorDescription = errorDescription
            }
        }, EaJsonOptions.Create());

        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Content-Type"] = "application/json",
                ["X-Correlation-ID"] = correlationId
            },
            Body = body
        };
    }
}