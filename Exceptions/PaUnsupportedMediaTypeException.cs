namespace MulecombineMainLambda;

public sealed class PaUnsupportedMediaTypeException : Exception
{
    public PaUnsupportedMediaTypeException(string message) : base(message)
    {
    }
}