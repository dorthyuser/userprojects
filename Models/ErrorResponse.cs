namespace AdverseEventReporter.Models;

public class ErrorResponse
{
    public string Status { get; set; } = "error";
    public string Code { get; set; }
    public string Message { get; set; }

    public ErrorResponse(string code, string message)
    {
        Code = code;
        Message = message;
    }
}
