namespace MulecombineMainLambda;

public sealed class EaBadRequestException : Exception
{
    public EaBadRequestException(string message) : base(message)
    {
    }
}