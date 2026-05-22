using System.Net;
using System.Threading;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Httptravelcardch104Lambda.Models;
using Httptravelcardch104Lambda.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Httptravelcardch104Lambda;

public class Function
{
    private static readonly Service _service = new();

    public async Task<APIGatewayProxyResponse> httptravelcardch104(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var clientId = GetHeaderValue(request.Headers, "client_id");
        context.Logger.LogLine($"Controller entry: method={request.HttpMethod}, route={request.Path}, client_id={clientId ?? string.Empty}");

        // Create a cancellation token that cancels slightly before the Lambda remaining time expires,
        // so we can return a controlled timeout response rather than letting AWS forcibly kill the function.
        using var cts = CreateCancellationTokenSourceFromContext(context);

        try
        {
            if (!string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                return BuildErrorResponse(HttpStatusCode.MethodNotAllowed, "httpMethod", "must be POST");
            }

            var body = request.Body ?? string.Empty;
            var apiResponse = await _service.ForwardAsync(body, request.Headers, cts.Token);

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)apiResponse.StatusCode,
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json"
                },
                Body = apiResponse.Body
            };
        }
        catch (InvalidOperationException ex)
        {
            context.Logger.LogLine($"Token generation or configuration failure: {ex.Message}");
            return BuildErrorResponse(HttpStatusCode.BadGateway, "processing", ex.Message);
        }
        catch (HttpRequestException ex)
        {
            context.Logger.LogLine($"External API failure: {ex.Message}");
            return BuildErrorResponse(HttpStatusCode.BadGateway, "external_api", "External API call failed.");
        }
        catch (OperationCanceledException ex)
        {
            // Catch OperationCanceledException (includes TaskCanceledException) so we reliably return a 504
            // before AWS forcibly times out the Lambda.
            context.Logger.LogLine($"Timeout while calling external API: {ex.Message}");
            return BuildErrorResponse(HttpStatusCode.GatewayTimeout, "timeout", "The request timed out.");
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Unhandled exception: {ex.Message}");
            return BuildErrorResponse(HttpStatusCode.InternalServerError, "internal", "An unexpected error occurred.");
        }
    }

    private static CancellationTokenSource CreateCancellationTokenSourceFromContext(ILambdaContext? context)
    {
        try
        {
            var remaining = context?.RemainingTime ?? TimeSpan.FromSeconds(120);
            // Leave a slightly larger buffer (5s) to allow handler to prepare response before AWS kills the function
            var ms = (int)Math.Max(100, remaining.TotalMilliseconds - 5000);
            return new CancellationTokenSource(ms);
        }
        catch
        {
            // In case of any issue, fall back to a conservative 115s timeout
            return new CancellationTokenSource(TimeSpan.FromSeconds(115));
        }
    }

    private static string? GetHeaderValue(IDictionary<string, string>? headers, string name)
    {
        if (headers is null)
        {
            return null;
        }

        foreach (var header in headers)
        {
            if (string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return header.Value;
            }
        }

        return null;
    }

    private static APIGatewayProxyResponse BuildErrorResponse(HttpStatusCode statusCode, string field, string message)
    {
        var response = new Response
        {
            Error = new ErrorResponse
            {
                Field = field,
                Message = message
            }
        };

        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            },
            Body = System.Text.Json.JsonSerializer.Serialize(response)
        };
    }
}
