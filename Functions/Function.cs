using System.Text;
using System.Text.Json;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace BusTravelAccountsLambda;

public sealed class Function
{
    private static readonly EaApiService Api = new();
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
        try
        {
            var path = BasePath.Strip(request.Path ?? "/", Settings.ListenerPath);
            var method = request.HttpMethod?.ToUpperInvariant() ?? string.Empty;
            var headers = request.Headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var query = request.QueryStringParameters;
            var body = request.Body;
            if (request.IsBase64Encoded && body != null) body = Encoding.UTF8.GetString(Convert.FromBase64String(body));
            var correlationId = CorrelationHelper.GetCorrelationId(headers);
            headers["X-Correlation-ID"] = correlationId;

            EaApiResult result;
            if (path.Equals("/alive", StringComparison.OrdinalIgnoreCase) && method == "GET") result = await Api.AliveAsync();
            else if (path.Equals("/ready", StringComparison.OrdinalIgnoreCase) && method == "GET") result = await Api.ReadyAsync();
            else if (path.Equals("/accounts", StringComparison.OrdinalIgnoreCase) && method == "GET") result = await Api.GetAccountsAsync(query, headers);
            else if (path.Equals("/accounts", StringComparison.OrdinalIgnoreCase) && method == "POST") result = await Api.PostAccountsAsync(body, query, headers);
            else if (path.StartsWith("/accounts/", StringComparison.OrdinalIgnoreCase) && method == "GET") result = await Api.GetAccountByIdAsync(path[10..], query, headers);
            else if (path.StartsWith("/accounts/", StringComparison.OrdinalIgnoreCase) && method == "PUT") result = await Api.PutAccountByIdAsync(path[10..], body, query, headers);
            else if (path.Equals("/accounts", StringComparison.OrdinalIgnoreCase)) return Error(405, "METHOD NOT ALLOWED", "error");
            else if (path.StartsWith("/accounts/", StringComparison.OrdinalIgnoreCase)) return Error(405, "METHOD NOT ALLOWED", "error");
            else return Error(404, "RESOURCE NOT FOUND", "error");

            return new APIGatewayProxyResponse { StatusCode = result.StatusCode, Body = result.Body, Headers = new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase) };
        }
        catch (Exception ex)
        {
            AppLog.Error("Unhandled error", ex);
            return Error(500, "MULE UNKNOWN ERROR", "An unexpected error occurred");
        }
    }

    private static APIGatewayProxyResponse Error(int status, string message, string description)
    {
        var body = JsonSerializer.Serialize(new EaErrorResponse { Error = new EaErrorDetail { ErrorCode = status, ErrorDateTime = DateTime.UtcNow, ErrorMessage = message, ErrorDescription = description } }, JsonHelper.Options);
        return new APIGatewayProxyResponse { StatusCode = status, Body = body, Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
    }
}
