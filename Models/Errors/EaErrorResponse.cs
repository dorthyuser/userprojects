namespace MuleaesaMainLambda;

public sealed class EaErrorResponse
{
    public EaErrorBody Error { get; set; } = new();
}

public sealed class EaErrorBody
{
    public int ErrorCode { get; set; }
    public DateTime ErrorDateTime { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorDescription { get; set; } = string.Empty;
}
