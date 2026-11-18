namespace BusTravelAccountsSaFunction;

public sealed class DatabaseWriteException(string sqlState, Exception innerException) : Exception(sqlState, innerException)
{
    public string SqlState { get; } = sqlState;
}