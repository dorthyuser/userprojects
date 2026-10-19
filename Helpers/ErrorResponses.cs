using System.Text.Json;

namespace BusTravelAccountsSaLambda;

public static class ErrorResponses
{
    public static APIGatewayProxyResponse BadRequest() => Json(400, "BAD REQUEST");
    public static APIGatewayProxyResponse NotFound() => Json(404, "RESOURCE NOT FOUND");
    public static APIGatewayProxyResponse MethodNotAllowed() => Json(405, "METHOD NOT ALLOWED");
    public static APIGatewayProxyResponse UnsupportedMediaType() => Json(415, "UNSUPPORTED MEDIA TYPE");
    public static APIGatewayProxyResponse InternalServerError() => Json(500, new ErrorResponse
    {
        Error = new ErrorDetails
        {
            ErrorCode = 500,
            ErrorDateTime = DateTime.UtcNow,
            ErrorMessage = "MULE UNKNOWN ERROR",
            ErrorDescription = "An unexpected error occurred"
        }
    });

    private static APIGatewayProxyResponse Json(int statusCode, object body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            Body = JsonSerializer.Serialize(body, JsonOptions.Default)
        };
    }
}