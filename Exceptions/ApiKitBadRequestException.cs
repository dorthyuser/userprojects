namespace BusTravelAccountsSaMainLambda;

public sealed class ApiKitBadRequestException : Exception
{
    public ApiKitBadRequestException(string message) : base(message)
    {
    }
}
