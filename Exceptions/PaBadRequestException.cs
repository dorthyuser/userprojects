namespace MulecombineMainLambda;

public sealed class PaBadRequestException : Exception
{
    public PaBadRequestException(string message) : base(message)
    {
    }
}