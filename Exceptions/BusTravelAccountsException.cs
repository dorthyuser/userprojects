namespace BusTravelAccountsLambda;

public sealed class BusTravelAccountsException : Exception
{
    public BusTravelAccountsException(string message, Exception? innerException = null) : base(message, innerException) { }
}