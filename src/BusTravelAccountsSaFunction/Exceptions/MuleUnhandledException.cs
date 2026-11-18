namespace BusTravelAccountsSaFunction;

public sealed class MuleUnhandledException : Exception
{
    public MuleUnhandledException(string message) : base(message)
    {
    }
}