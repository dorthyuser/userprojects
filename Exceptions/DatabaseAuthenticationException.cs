namespace BusTravelAccountsSaMainLambda;

public sealed class DatabaseAuthenticationException : Exception
{
    public DatabaseAuthenticationException(string message, string safeDescription) : base(message)
    {
        SafeDescription = safeDescription;
    }

    public string SafeDescription { get; }
}