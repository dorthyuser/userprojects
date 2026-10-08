namespace BusTravelAccountsSaMainLambda;

public static class ResponseHelper
{
    public static APIGatewayProxyResponse CreateJsonResponse(int statusCode, string? body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Headers = CreateHeaders(),
            Body = body ?? string.Empty
        };
    }

    public static Dictionary<string, string> CreateHeaders()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Content-Type"] = "application/json"
        };
    }

    public static Dictionary<string, string> CreatePlainTextHeaders()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Content-Type"] = "text/plain"
        };
    }
}