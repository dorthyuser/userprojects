namespace MulecombineMainLambda;

public sealed class PaNotFoundException : Exception
{
    public PaNotFoundException(string message) : base(message)
    {
    }
}