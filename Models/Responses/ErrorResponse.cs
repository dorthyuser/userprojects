namespace BusTravelAccountsSaLambda;

public sealed class ErrorResponse
{
    public ErrorDetails? Error { get; set; }
}

public sealed class ErrorDetails
{
    public int ErrorCode { get; set; }
    public DateTime ErrorDateTime { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorDescription { get; set; }
}
