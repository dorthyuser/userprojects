using System.Text;
using System.Text.Json;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace BusTravelAccountsLambda;

public sealed class Function
{
    private static readonly EaApiService Service = new();

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
            var settings = EaSettings.Current();
            var path = BasePath.Strip(request.Path, settings.ListenerPath);
            var headers = request.Headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var body = request.IsBase64Encoded && request.Body is not null ? Encoding.UTF8.GetString(Convert.FromBase64String(request.Body)) : request.Body;
            var method = request.HttpMethod?.ToUpperInvariant() ?? string.Empty;

            if (path == "/alive") return ToResponse(await Service.AliveAsync());
            if (path == "/ready") return ToResponse(await Service.ReadyAsync());
            if (path == "/accounts" && method == "GET") return ToResponse(await Service.GetAccountsAsync(GetQueryStringValue(request.QueryStringParameters, "email"), headers));
            if (path == "/accounts" && method == "POST")
            {
                if (!IsJsonContentType(headers)) return UnsupportedMediaType();
                if (!IsValidJson(body)) return BadRequest();
                return ToResponse(await Service.PostAccountsAsync(body, headers));
            }
            if (path.StartsWith("/accounts/", StringComparison.OrdinalIgnoreCase) && method == "GET") return ToResponse(await Service.GetAccountDetailsAsync(path[10..], headers));
            if (path.StartsWith("/accounts/", StringComparison.OrdinalIgnoreCase) && method == "PUT")
            {
                if (!IsJsonContentType(headers)) return UnsupportedMediaType();
                if (!IsValidJson(body)) return BadRequest();
                return ToResponse(await Service.PutAccountsAsync(path[10..], body, headers));
            }
            if (path == "/accounts") return MethodNotAllowed();
            if (path.StartsWith("/accounts/", StringComparison.OrdinalIgnoreCase)) return MethodNotAllowed();
            return NotFound();
        }
        catch (Exception ex)
        {
            AppLog.Error("Unhandled error", ex);
            return new APIGatewayProxyResponse { StatusCode = 500, Body = JsonSerializer.Serialize(new EaErrorResponse { Error = new EaErrorBody { ErrorCode = 500, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "MULE UNKNOWN ERROR", ErrorDescription = "An unexpected error occurred" } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
    }

    private static string? GetQueryStringValue(IDictionary<string, string>? queryStringParameters, string key)
    {
        if (queryStringParameters is null) return null;
        return queryStringParameters.TryGetValue(key, out var value) ? value : null;
    }

    private static bool IsJsonContentType(IDictionary<string, string> headers)
    {
        var contentType = HeaderHelper.GetHeader(headers, "Content-Type");
        if (string.IsNullOrWhiteSpace(contentType)) return false;
        var mediaType = contentType.Split(';', 2, StringSplitOptions.TrimEntries)[0];
        return string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidJson(string? body)
    {
        if (body is null) return true;
        try
        {
            using var _ = JsonDocument.Parse(body);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static APIGatewayProxyResponse BadRequest() => new() { StatusCode = 400, Body = JsonSerializer.Serialize(new EaErrorResponse { Error = new EaErrorBody { ErrorCode = 400, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "BAD REQUEST", ErrorDescription = "Bad request" } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
    private static APIGatewayProxyResponse NotFound() => new() { StatusCode = 404, Body = JsonSerializer.Serialize(new EaErrorResponse { Error = new EaErrorBody { ErrorCode = 404, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "RESOURCE NOT FOUND", ErrorDescription = "Unknown path" } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
    private static APIGatewayProxyResponse MethodNotAllowed() => new() { StatusCode = 405, Body = JsonSerializer.Serialize(new EaErrorResponse { Error = new EaErrorBody { ErrorCode = 405, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "METHOD NOT ALLOWED", ErrorDescription = "Method not allowed" } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
    private static APIGatewayProxyResponse UnsupportedMediaType() => new() { StatusCode = 415, Body = JsonSerializer.Serialize(new EaErrorResponse { Error = new EaErrorBody { ErrorCode = 415, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "UNSUPPORTED MEDIA TYPE", ErrorDescription = "Unsupported media type" } }, JsonHelper.Options), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
    private static APIGatewayProxyResponse ToResponse(EaApiResult result) => new() { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
}
