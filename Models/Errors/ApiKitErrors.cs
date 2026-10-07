namespace BusTravelAccountsSaMainLambda;

public static class ErrorResponseFactory
{
    public static ErrorResponse BadRequest(string description) => Create(400, "BAD REQUEST", description);
    public static ErrorResponse NotFound(string description) => Create(404, "RESOURCE NOT FOUND", description);
    public static ErrorResponse MethodNotAllowed(string description) => Create(405, "METHOD NOT ALLOWED", description);
    public static ErrorResponse UnsupportedMediaType(string description) => Create(415, "UNSUPPORTED MEDIA TYPE", description);
    public static ErrorResponse InternalServerError(string description) => Create(500, "ANY ERROR", description);

    private static ErrorResponse Create(int code, string message, string description) => new()
    {
        Error = new ErrorBody
        {
            ErrorCode = code,
            ErrorDateTime = DateTime.UtcNow,
            ErrorMessage = message,
            ErrorDescription = description
        }
    };
}
