using System.Text.Json;

namespace BusTravelAccountsSaLambda;

public static class ResponseHelper
{
    public static APIGatewayProxyResponse Text(int statusCode, string body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string> { ["Content-Type"] = "text/plain" },
            Body = body
        };
    }

    public static APIGatewayProxyResponse Json(int statusCode, object body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            Body = JsonSerializer.Serialize(body, JsonOptions.Default)
        };
    }
}