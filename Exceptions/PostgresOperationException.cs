namespace BusTravelAccountsSaMainLambda;

public sealed class PostgresOperationException : Exception
{
    public PostgresOperationException(string sqlState, string safeDescription) : base(sqlState + " ERROR")
    {
        SqlState = sqlState;
        SafeDescription = safeDescription;
    }

    public string SqlState { get; }
    public string SafeDescription { get; }
}