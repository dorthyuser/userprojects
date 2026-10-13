using System.Text.Json;

namespace BusTravelAccountsSaLambda;

public static class ResponseFactory
{
    public static APIGatewayProxyResponse Json<T>(int statusCode, T body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "application/json" },
            Body = JsonHelper.Serialize(body)
        };
    }

    public static APIGatewayProxyResponse Text(int statusCode, string body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "text/plain" },
            Body = body
        };
    }

    public static APIGatewayProxyResponse NoContentArray() => Json(204, Array.Empty<object>());
    public static APIGatewayProxyResponse AccountNotFound() => Json(400, new ErrorResponse { Error = new ErrorDetails { ErrorCode = 400, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "Account not found", ErrorDescription = "Account not found" } });
    public static APIGatewayProxyResponse DatabaseError(string sqlState, string message) => Json(400, new ErrorResponse { Error = new ErrorDetails { ErrorCode = 400, ErrorDateTime = DateTime.UtcNow, ErrorMessage = string.IsNullOrWhiteSpace(sqlState) ? "ERROR" : sqlState + " ERROR", ErrorDescription = message } });
    public static APIGatewayProxyResponse BadRequest() => Json(400, new ErrorResponse { Error = new ErrorDetails { ErrorCode = 400, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "BAD REQUEST", ErrorDescription = "BAD REQUEST" } });
    public static APIGatewayProxyResponse NotFound() => Json(404, new ErrorResponse { Error = new ErrorDetails { ErrorCode = 404, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "RESOURCE NOT FOUND", ErrorDescription = "RESOURCE NOT FOUND" } });
    public static APIGatewayProxyResponse MethodNotAllowed() => Json(405, new ErrorResponse { Error = new ErrorDetails { ErrorCode = 405, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "METHOD NOT ALLOWED", ErrorDescription = "METHOD NOT ALLOWED" } });
    public static APIGatewayProxyResponse UnsupportedMediaType() => Json(415, new ErrorResponse { Error = new ErrorDetails { ErrorCode = 415, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "UNSUPPORTED MEDIA TYPE", ErrorDescription = "UNSUPPORTED MEDIA TYPE" } });
    public static APIGatewayProxyResponse InternalServerError() => Json(500, new ErrorResponse { Error = new ErrorDetails { ErrorCode = 500, ErrorDateTime = DateTime.UtcNow, ErrorMessage = "An unexpected error occurred", ErrorDescription = "An unexpected error occurred" } });
}
