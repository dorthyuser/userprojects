namespace BusTravelAccountsSaMainLambda;

public sealed class ApiKitMethodNotAllowedException : Exception
{
    public ApiKitMethodNotAllowedException(string message) : base(message)
    {
    }
}
