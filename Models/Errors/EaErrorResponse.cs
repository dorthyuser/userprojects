namespace BusTravelAccountsLambda;

public sealed class EaErrorResponse
{
    public EaErrorDetails Error { get; init; } = new();
}

public sealed class EaErrorDetails
{
    public int ErrorCode { get; init; }
    public DateTime ErrorDateTime { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public string ErrorDescription { get; init; } = string.Empty;
}