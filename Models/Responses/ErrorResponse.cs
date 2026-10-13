namespace BusTravelAccountsSaLambda;

public sealed class ErrorResponse
{
    public ErrorDetails Error { get; set; } = new();
}

public sealed class ErrorDetails
{
    public int ErrorCode { get; set; }
    public DateTime ErrorDateTime { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorDescription { get; set; } = string.Empty;
}