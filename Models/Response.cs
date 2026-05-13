namespace Httptravelcardch104Lambda.Models;

public class Response
{
    public ErrorResponse? Error { get; set; }
    public string? GeneratedId { get; set; }
}

public class ErrorResponse
{
    public string? Field { get; set; }
    public string? Message { get; set; }
}