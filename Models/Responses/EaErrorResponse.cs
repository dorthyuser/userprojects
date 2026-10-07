namespace MulecombineMainLambda;

public sealed class EaErrorResponse
{
    public EaErrorBody? Error { get; set; }
}

public sealed class EaErrorBody
{
    public int ErrorCode { get; set; }
    public DateTimeOffset ErrorDateTime { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorDescription { get; set; }
}