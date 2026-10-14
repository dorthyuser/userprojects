namespace BusTravelAccountsLambda;

public sealed class EaErrorResponse
{
    public EaErrorDetail Error { get; set; } = new();
}

public sealed class EaErrorDetail
{
    public int ErrorCode { get; set; }
    public DateTime ErrorDateTime { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorDescription { get; set; } = string.Empty;
}