namespace LambdacsharphttpLambda.Models;

public sealed class Response
{
    public bool Success { get; set; }
    public string? Data { get; set; }
    public ErrorResponse? Error { get; set; }
}

public sealed class ErrorResponse
{
    public string? Code { get; set; }
    public string? Message { get; set; }
}