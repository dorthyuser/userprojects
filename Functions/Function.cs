using System.Text;
using System.Text.Json;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace BusTravelAccountsSaLambda;

public sealed class Function
{
    private static readonly AccountsService Service = new();

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
            var response = await HandleRequestAsync(request, context).ConfigureAwait(false);
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
            var path = request.Path ?? "/";
            var stripped = BasePath.Strip(path, Settings.Current.ListenerPath);
            var method = request.HttpMethod?.ToUpperInvariant() ?? string.Empty;
            var body = DecodeBody(request);

            if (HasUnsupportedJsonContentType(method, stripped, request.Headers, body))
            {
                var unsupported = new ApiResult
                {
                    StatusCode = 415,
                    Body = JsonSerializer.Serialize(new ErrorResponse
                    {
                        Error = new ErrorDetails
                        {
                            ErrorCode = 415,
                            ErrorDateTime = DateTime.UtcNow,
                            ErrorMessage = "UNSUPPORTED MEDIA TYPE",
                            ErrorDescription = "Unsupported media type"
                        }
                    }, JsonOptions.Default),
                    Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "application/json" }
                };

                return new APIGatewayProxyResponse
                {
                    StatusCode = unsupported.StatusCode,
                    Body = unsupported.Body,
                    Headers = unsupported.Headers
                };
            }

            ApiResult result;
            if (IsAlive(stripped))
            {
                result = Service.Alive();
            }
            else if (IsReady(stripped))
            {
                result = Service.Ready();
            }
            else if (method == "GET" && stripped.Equals("/accounts", StringComparison.OrdinalIgnoreCase))
            {
                result = await Service.GetAccountsAsync(request.QueryStringParameters, CancellationToken.None).ConfigureAwait(false);
            }
            else if (method == "GET" && TryMatchAccountId(stripped, out var id))
            {
                result = await Service.GetAccountByIdAsync(id!, CancellationToken.None).ConfigureAwait(false);
            }
            else if (method == "POST" && stripped.Equals("/accounts", StringComparison.OrdinalIgnoreCase))
            {
                result = await Service.CreateAccountAsync(body, CancellationToken.None).ConfigureAwait(false);
            }
            else if (method == "PUT" && TryMatchAccountId(stripped, out var updateId))
            {
                result = await Service.UpdateAccountAsync(updateId!, body, CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                result = BuildRoutingError(stripped, method, request);
            }

            return new APIGatewayProxyResponse
            {
                StatusCode = result.StatusCode,
                Body = result.Body,
                Headers = result.Headers
            };
        }
        catch (Exception ex)
        {
            AppLog.Error("Unhandled request processing error", ex);
            var error = new ErrorResponse
            {
                Error = new ErrorDetails
                {
                    ErrorCode = 500,
                    ErrorDateTime = DateTime.UtcNow,
                    ErrorMessage = "MULE UNKNOWN ERROR",
                    ErrorDescription = "An unexpected error occurred"
                }
            };

            return new APIGatewayProxyResponse
            {
                StatusCode = 500,
                Body = JsonSerializer.Serialize(error, JsonOptions.Default),
                Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "application/json" }
            };
        }
    }

    private static bool HasUnsupportedJsonContentType(string method, string path, IDictionary<string, string>? headers, string? body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return false;
        }

        if (!(method == "POST" && path.Equals("/accounts", StringComparison.OrdinalIgnoreCase)) && !(method == "PUT" && TryMatchAccountId(path, out _)))
        {
            return false;
        }

        var contentType = GetHeader(headers, "Content-Type");
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        var mediaType = contentType.Split(';', 2, StringSplitOptions.TrimEntries)[0];
        return !mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetHeader(IDictionary<string, string>? headers, string name)
    {
        if (headers is null)
        {
            return null;
        }

        return headers.TryGetValue(name, out var value) ? value : null;
    }

    private static bool IsAlive(string path) => path.Equals("/alive", StringComparison.OrdinalIgnoreCase);
    private static bool IsReady(string path) => path.Equals("/ready", StringComparison.OrdinalIgnoreCase);

    private static bool TryMatchAccountId(string path, out string? id)
    {
        id = null;
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 && parts[0].Equals("accounts", StringComparison.OrdinalIgnoreCase))
        {
            id = parts[1];
            return true;
        }

        return false;
    }

    private static string? DecodeBody(APIGatewayProxyRequest request)
    {
        if (string.IsNullOrEmpty(request.Body))
        {
            return null;
        }

        if (request.IsBase64Encoded)
        {
            var bytes = Convert.FromBase64String(request.Body);
            return Encoding.UTF8.GetString(bytes);
        }

        return request.Body;
    }

    private static ApiResult BuildRoutingError(string path, string method, APIGatewayProxyRequest request)
    {
        if (path.Equals("/accounts", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiResult { StatusCode = 405, Body = JsonSerializer.Serialize(new ErrorResponse { Error = new ErrorDetails { ErrorCode = 405, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "METHOD NOT ALLOWED", ErrorDescription = "Method not allowed" } }, JsonOptions.Default), Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "application/json" } };
        }

        if (path.StartsWith("/accounts/", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiResult { StatusCode = 405, Body = JsonSerializer.Serialize(new ErrorResponse { Error = new ErrorDetails { ErrorCode = 405, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "METHOD NOT ALLOWED", ErrorDescription = "Method not allowed" } }, JsonOptions.Default), Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "application/json" } };
        }

        if (path.Equals("/alive", StringComparison.OrdinalIgnoreCase) || path.Equals("/ready", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiResult { StatusCode = 405, Body = "DOWN", Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "text/plain" } };
        }

        return new ApiResult { StatusCode = 404, Body = JsonSerializer.Serialize(new ErrorResponse { Error = new ErrorDetails { ErrorCode = 404, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "RESOURCE NOT FOUND", ErrorDescription = "Resource not found" } }, JsonOptions.Default), Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "application/json" } };
    }
}