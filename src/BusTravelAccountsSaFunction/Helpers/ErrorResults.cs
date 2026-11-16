using Microsoft.AspNetCore.Mvc;

namespace BusTravelAccountsSaFunction;

public static class ErrorResults
{
    public static IActionResult BadRequest(string description) =>
        new ObjectResult(ErrorResponse.Create(400, "BAD REQUEST", description)) { StatusCode = 400 };

    public static IActionResult ResourceNotFound() =>
        new ObjectResult(ErrorResponse.Create(404, "RESOURCE NOT FOUND", "Resource not found")) { StatusCode = 404 };

    public static IActionResult MethodNotAllowed() =>
        new ObjectResult(ErrorResponse.Create(405, "METHOD NOT ALLOWED", "Method not allowed")) { StatusCode = 405 };

    public static IActionResult UnsupportedMediaType(string description) =>
        new ObjectResult(ErrorResponse.Create(415, "UNSUPPORTED MEDIA TYPE", description)) { StatusCode = 415 };

    public static IActionResult SalesforceConnectivityInvalid(string description) =>
        new ObjectResult(ErrorResponse.Create(401, "SALESFORCE CONNECTIVITY ERROR", description)) { StatusCode = 401 };

    public static IActionResult SalesforceConnectivityFailedToSendRequest(string description) =>
        new ObjectResult(ErrorResponse.Create(404, "SALESFORCE CONNECTIVITY ERROR", description)) { StatusCode = 404 };

    public static IActionResult SalesforceOperationFailed(string statusCode, string description) =>
        new ObjectResult(ErrorResponse.Create(400, $"{statusCode} ERROR", description)) { StatusCode = 400 };

    public static IActionResult Unexpected() =>
        new ObjectResult(ErrorResponse.Create(500, "MULE UNKNOWN ERROR", "An unexpected error occurred")) { StatusCode = 500 };

    public static IActionResult MuleUnknown(string description) =>
        new ObjectResult(ErrorResponse.Create(500, "MULE UNKNOWN ERROR", description)) { StatusCode = 500 };
}
