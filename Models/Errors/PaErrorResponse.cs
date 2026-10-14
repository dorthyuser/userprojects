namespace BusTravelAccountsLambda;

public sealed class PaErrorResponse
{
    public PaErrorDetails Error { get; init; } = new();
}

public sealed class PaErrorDetails
{
    public int ErrorCode { get; init; }
    public DateTime ErrorDateTime { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public string ErrorDescription { get; init; } = string.Empty;
}