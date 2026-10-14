namespace BusTravelAccountsLambda;

public sealed class PaApiException : Exception
{
    public PaApiException(string message) : base(message) { }
}