namespace BusTravelAccountsSaFunction;

public sealed class MuleUnknownException : Exception
{
    public MuleUnknownException(string description) : base(description)
    {
    }
}