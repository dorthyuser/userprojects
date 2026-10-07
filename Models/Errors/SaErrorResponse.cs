namespace MuleaesaMainLambda;

public sealed class SaErrorResponse
{
    public SaErrorBody Error { get; set; } = new();
}

public sealed class SaErrorBody
{
    public int ErrorCode { get; set; }
    public DateTime ErrorDateTime { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorDescription { get; set; } = string.Empty;
}