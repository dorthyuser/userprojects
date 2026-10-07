namespace MulecombineMainLambda;

public sealed class SaMethodNotAllowedException : Exception
{
    public SaMethodNotAllowedException(string message) : base(message)
    {
    }
}