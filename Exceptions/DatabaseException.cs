namespace Azfcsharp8Function;

public sealed class DatabaseException : Exception
{
    public string SqlState { get; }
    public string DatabaseMessage { get; }

    public DatabaseException(string sqlState, string databaseMessage)
    {
        SqlState = sqlState;
        DatabaseMessage = databaseMessage;
    }
}
