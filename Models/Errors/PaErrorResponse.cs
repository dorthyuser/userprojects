namespace BusTravelAccountsLambda;

public sealed class PaErrorResponse
{
    public PaErrorBody Error { get; init; } = new();
}

public sealed class PaErrorBody
{
    public int ErrorCode { get; init; }
    public DateTime ErrorDateTime { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public string ErrorDescription { get; init; } = string.Empty;
}