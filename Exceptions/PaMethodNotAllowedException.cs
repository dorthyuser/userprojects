namespace MulecombineMainLambda;

public sealed class PaMethodNotAllowedException : Exception
{
    public PaMethodNotAllowedException(string message) : base(message)
    {
    }
}