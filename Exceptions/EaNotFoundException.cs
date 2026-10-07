namespace MulecombineMainLambda;

public sealed class EaNotFoundException : Exception
{
    public EaNotFoundException(string message) : base(message)
    {
    }
}