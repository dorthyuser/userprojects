namespace BusTravelAccountsSaMainLambda;

public sealed class ApiKitUnsupportedMediaTypeException : Exception
{
    public ApiKitUnsupportedMediaTypeException(string message) : base(message)
    {
    }
}
