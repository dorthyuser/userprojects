using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Httptravelcardch104Lambda.Models;
using Httptravelcardch104Lambda.Services;

namespace Httptravelcardch104Lambda;

public class Function
{
    private static readonly Service _service = new();

    public async Task<APIGatewayProxyResponse> httptravelcardch104(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var clientId = GetHeaderValue(request.Headers, "client_id");
        context.Logger.LogLine($"Controller entry: method={request.HttpMethod}, route={request.Path}, client_id={clientId ?? string.Empty}");

        try
        {
            if (!string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                return BuildErrorResponse(HttpStatusCode.MethodNotAllowed, "httpMethod", "must be POST");
            }

            var body = request.Body ?? string.Empty;
            var apiResponse = await _service.ForwardAsync(body, request.Headers, CancellationToken.None);

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
        catch (TaskCanceledException ex)
        {
            context.Logger.LogLine($"Timeout while calling external API: {ex.Message}");
            return BuildErrorResponse(HttpStatusCode.GatewayTimeout, "timeout", "The request timed out.");
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Unhandled exception: {ex.Message}");
            return BuildErrorResponse(HttpStatusCode.InternalServerError, "internal", "An unexpected error occurred.");
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