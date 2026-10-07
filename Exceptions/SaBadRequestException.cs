namespace MuleaesaMainLambda;

public sealed class SaBadRequestException : Exception
{
    public SaBadRequestException(string message) : base(message)
    {
    }
}