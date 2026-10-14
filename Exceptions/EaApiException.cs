namespace BusTravelAccountsLambda;

public sealed class EaApiException : Exception
{
    public EaApiException(string message) : base(message) { }
}