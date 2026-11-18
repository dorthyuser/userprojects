using Microsoft.AspNetCore.Mvc;

namespace BusTravelAccountsSaFunction;

public static class ErrorResults
{
    public static IActionResult ResourceNotFound() =>
        new ObjectResult(new ErrorResponse
        {
            Error = new ErrorBody
            {
                ErrorCode = 404,
                ErrorDateTime = DateTimeOffset.UtcNow,
                ErrorMessage = "RESOURCE NOT FOUND",
                ErrorDescription = "Resource not found"
            }
        })
        {
            StatusCode = 404
        };

    public static IActionResult MethodNotAllowed() =>
        new ObjectResult(new ErrorResponse
        {
            Error = new ErrorBody
            {
                ErrorCode = 405,
                ErrorDateTime = DateTimeOffset.UtcNow,
                ErrorMessage = "METHOD NOT ALLOWED",
                ErrorDescription = "Method not allowed"
            }
        })
        {
            StatusCode = 405
        };

    public static IActionResult Unexpected() =>
        new ObjectResult(new ErrorResponse
        {
            Error = new ErrorBody
            {
                ErrorCode = 500,
                ErrorDateTime = DateTimeOffset.UtcNow,
                ErrorMessage = "MULE UNKNOWN ERROR",
                ErrorDescription = "An unexpected error occurred"
            }
        })
        {
            StatusCode = 500
        };

    public static IActionResult BadRequest(string description) =>
        new ObjectResult(new ErrorResponse
        {
            Error = new ErrorBody
            {
                ErrorCode = 400,
                ErrorDateTime = DateTimeOffset.UtcNow,
                ErrorMessage = "BAD REQUEST",
                ErrorDescription = description
            }
        })
        {
            StatusCode = 400
        };

    public static IActionResult UnsupportedMediaType() =>
        new ObjectResult(new ErrorResponse
        {
            Error = new ErrorBody
            {
                ErrorCode = 415,
                ErrorDateTime = DateTimeOffset.UtcNow,
                ErrorMessage = "UNSUPPORTED MEDIA TYPE",
                ErrorDescription = "Unsupported media type"
            }
        })
        {
            StatusCode = 415
        };

    public static IActionResult DatabaseFailure(string sqlState, string description) =>
        new ObjectResult(new ErrorResponse
        {
            Error = new ErrorBody
            {
                ErrorCode = 400,
                ErrorDateTime = DateTimeOffset.UtcNow,
                ErrorMessage = $"{sqlState} ERROR",
                ErrorDescription = description
            }
        })
        {
            StatusCode = 400
        };

    public static IActionResult MuleUnknown(string description) =>
        new ObjectResult(new ErrorResponse
        {
            Error = new ErrorBody
            {
                ErrorCode = 500,
                ErrorDateTime = DateTimeOffset.UtcNow,
                ErrorMessage = "MULE UNKNOWN ERROR",
                ErrorDescription = description
            }
        })
        {
            StatusCode = 500
        };

    public static IActionResult MuleUnhandled(string message) =>
        new ObjectResult(new ErrorResponse
        {
            Error = new ErrorBody
            {
                ErrorCode = 500,
                ErrorDateTime = DateTimeOffset.UtcNow,
                ErrorMessage = message,
                ErrorDescription = "An unexpected error occurred"
            }
        })
        {
            StatusCode = 500
        };
}