namespace MulecombineMainLambda;

public sealed class EaMethodNotAllowedException : Exception
{
    public EaMethodNotAllowedException(string message) : base(message)
    {
    }
}