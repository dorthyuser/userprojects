namespace MulecombineMainLambda;

public sealed class SaNotFoundException : Exception
{
    public SaNotFoundException(string message) : base(message)
    {
    }
}