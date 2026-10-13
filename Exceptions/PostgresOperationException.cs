namespace BusTravelAccountsSaLambda;

public sealed class PostgresOperationException : Exception
{
    public PostgresOperationException(string sqlState, string message, Exception innerException) : base(message, innerException)
    {
        SqlState = sqlState;
    }

    public string SqlState { get; }
}
