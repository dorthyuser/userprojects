using System.Text.Json;

namespace MuleaesaMainLambda;

public static class ResponseHelper
{
    public static APIGatewayProxyResponse ToResponse(EaApiResult result, string correlationId)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in result.Headers)
        {
            headers[header.Key] = header.Value;
        }

        headers["X-Correlation-ID"] = correlationId;

        return new APIGatewayProxyResponse
        {
            StatusCode = result.StatusCode,
            Headers = headers,
            Body = result.Body
        };
    }

    public static APIGatewayProxyResponse ToErrorResponse(int statusCode, string errorMessage, string errorDescription)
    {
        var payload = new EaErrorResponse
        {
            Error = new EaErrorBody
            {
                ErrorCode = statusCode,
                ErrorDateTime = DateTime.UtcNow,
                ErrorMessage = errorMessage,
                ErrorDescription = errorDescription
            }
        };

        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Content-Type"] = "application/json"
            },
            Body = JsonSerializer.Serialize(payload, JsonOptions.Default)
        };
    }
}
