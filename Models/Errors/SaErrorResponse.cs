namespace BusTravelAccountsLambda;

public sealed class SaErrorResponse
{
    public SaErrorBody Error { get; init; } = new();
}

public sealed class SaErrorBody
{
    public int ErrorCode { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public string ErrorDescription { get; init; } = string.Empty;
}