namespace BusTravelAccountsSaMainLambda;

public sealed class DatabaseConnectivityException : Exception
{
    public DatabaseConnectivityException(string message, string safeDescription) : base(message)
    {
        SafeDescription = safeDescription;
    }

    public string SafeDescription { get; }
}