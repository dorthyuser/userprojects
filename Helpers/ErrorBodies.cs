using System.Text.Json;

namespace BusTravelAccountsSaLambda;

public static class ErrorBodies
{
    public static ApiResult BadRequest(string description) => Json(400, "BAD REQUEST", description);
    public static ApiResult NotFound() => Json(404, "RESOURCE NOT FOUND", "Resource not found");
    public static ApiResult MethodNotAllowed() => Json(405, "METHOD NOT ALLOWED", "Method not allowed");
    public static ApiResult UnsupportedMediaType() => Json(415, "UNSUPPORTED MEDIA TYPE", "Unsupported media type");
    public static ApiResult Down() => ApiResult.Text(500, "DOWN");
    public static ApiResult AccountNotFound() => Json(400, "Account not found", "Account not found");
    public static ApiResult DatabaseError(string sqlState, string description) => Json(400, $"{sqlState} ERROR", description);
    public static ApiResult Unexpected() => Json(500, "MULE UNKNOWN ERROR", "An unexpected error occurred");

    private static ApiResult Json(int statusCode, string message, string description)
    {
        var body = JsonSerializer.Serialize(new ErrorResponse
        {
            Error = new ErrorDetails
            {
                ErrorCode = statusCode,
                ErrorDateTime = DateTime.UtcNow,
                ErrorMessage = message,
                ErrorDescription = description
            }
        }, JsonOptions.Default);
        return ApiResult.Json(statusCode, body);
    }
}
