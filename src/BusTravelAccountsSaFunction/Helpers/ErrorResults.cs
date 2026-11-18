using Microsoft.AspNetCore.Mvc;

namespace BusTravelAccountsSaFunction;

public static class ErrorResults
{
    public static IActionResult BadRequest() => Create(400, "BAD REQUEST", "Bad request");

    public static IActionResult ResourceNotFound() => Create(404, "RESOURCE NOT FOUND", "Resource not found");

    public static IActionResult MethodNotAllowed() => Create(405, "METHOD NOT ALLOWED", "Method not allowed");

    public static IActionResult UnsupportedMediaType() => Create(415, "UNSUPPORTED MEDIA TYPE", "Unsupported media type");

    public static IActionResult Unexpected() => Create(500, "MULE UNKNOWN ERROR", "An unexpected error occurred");

    public static IActionResult MuleUnknownPayloadError() => Create(500, "MULE UNKNOWN ERROR", "check payload existence or method/payload combination requirement");

    public static IActionResult DatabaseWriteFailure(string sqlState) => Create(400, $"{sqlState} ERROR", "Failed to write account");

    private static IActionResult Create(int statusCode, string errorMessage, string errorDescription) =>
        new ObjectResult(new ErrorEnvelope
        {
            Error = new ErrorBody
            {
                ErrorCode = statusCode,
                ErrorDateTime = DateTimeOffset.UtcNow,
                ErrorMessage = errorMessage,
                ErrorDescription = errorDescription
            }
        })
        {
            StatusCode = statusCode
        };
}