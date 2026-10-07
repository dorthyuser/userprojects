namespace MulecombineMainLambda;

public sealed class PaErrorResponse
{
    public PaErrorBody? Error { get; set; }
}

public sealed class PaErrorBody
{
    public int ErrorCode { get; set; }
    public DateTimeOffset ErrorDateTime { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorDescription { get; set; }
}