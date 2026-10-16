using System.Text;
using System.Text.Json;
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace BusTravelAccountsLambda;

public sealed class Function
{
    private static readonly EaApiService Service = new();
    private static readonly string ListenerPath = "api/v1/*";

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
        var stripped = BasePath.Strip(path, ListenerPath);
        var headers = request.Headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var query = request.QueryStringParameters;
        var method = request.HttpMethod?.ToUpperInvariant() ?? string.Empty;
        var body = request.Body;
        if (request.IsBase64Encoded && !string.IsNullOrEmpty(body)) body = Encoding.UTF8.GetString(Convert.FromBase64String(body));
        var correlationId = CorrelationHelper.GetCorrelationId(headers);
        headers["X-Correlation-ID"] = correlationId;
        try
        {
            if (stripped == "/alive") return ToResponse(await Service.AliveAsync());
            if (stripped == "/ready") return ToResponse(await Service.ReadyAsync());
            if (stripped == "/accounts" && method == "GET") return ToResponse(await Service.GetAccountsAsync(query, headers));
            if (stripped == "/accounts" && method == "POST")
            {
                if (!HasJsonContentType(headers)) return Error(415, "UNSUPPORTED MEDIA TYPE");
                if (!IsValidJson(body)) return Error(400, "BAD REQUEST");
                return ToResponse(await Service.PostAccountsAsync(body, query, headers));
            }
            if (stripped.StartsWith("/accounts/", StringComparison.Ordinal) && method == "GET") return ToResponse(await Service.GetAccountByIdAsync(stripped[("/accounts/".Length)..], query, headers));
            if (stripped.StartsWith("/accounts/", StringComparison.Ordinal) && method == "PUT")
            {
                if (!HasJsonContentType(headers)) return Error(415, "UNSUPPORTED MEDIA TYPE");
                if (!IsValidJson(body)) return Error(400, "BAD REQUEST");
                return ToResponse(await Service.PutAccountByIdAsync(stripped[("/accounts/".Length)..], body, query, headers));
            }
            if (stripped.StartsWith("/accounts", StringComparison.Ordinal) && (method == "GET" || method == "POST" || method == "PUT")) return Error(405, "METHOD NOT ALLOWED");
            return Error(404, "RESOURCE NOT FOUND");
        }
        catch (Exception ex)
        {
            AppLog.Error("Unhandled exception", ex);
            return Error(500, "MULE UNKNOWN ERROR");
        }
    }

    private static bool HasJsonContentType(IDictionary<string, string> headers)
    {
        var contentType = HeaderHelper.GetHeader(headers, "Content-Type");
        if (string.IsNullOrWhiteSpace(contentType)) return false;
        var mediaType = contentType.Split(';', 2)[0].Trim();
        return string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidJson(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return false;
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

    private static APIGatewayProxyResponse ToResponse(EaApiResult result) => new()
    {
        StatusCode = result.StatusCode,
        Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = result.Headers.TryGetValue("Content-Type", out var ct) ? ct : (result.Body != null && (result.Body.StartsWith("{") || result.Body.StartsWith("[")) ? "application/json" : "text/plain") },
        Body = result.Body
    };

    private static APIGatewayProxyResponse Error(int status, string message)
    {
        var body = JsonHelper.Serialize(new EaErrorResponse { Error = new EaErrorBody { ErrorCode = status, ErrorMessage = message, ErrorDescription = message == "MULE UNKNOWN ERROR" ? "An unexpected error occurred" : message } });
        return new APIGatewayProxyResponse { StatusCode = status, Body = body, Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
    }
}
