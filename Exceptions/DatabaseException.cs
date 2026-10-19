namespace BusTravelAccountsSaLambda;

public sealed class DatabaseException : Exception
{
    public DatabaseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}