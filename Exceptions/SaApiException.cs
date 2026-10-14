namespace BusTravelAccountsLambda;

public sealed class SaApiException : Exception
{
    public SaApiException(string message) : base(message) { }
}