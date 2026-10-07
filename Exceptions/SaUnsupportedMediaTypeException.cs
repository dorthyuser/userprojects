namespace MuleaesaMainLambda;

public sealed class SaUnsupportedMediaTypeException : Exception
{
    public SaUnsupportedMediaTypeException(string message) : base(message)
    {
    }
}