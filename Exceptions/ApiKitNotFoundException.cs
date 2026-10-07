namespace BusTravelAccountsSaMainLambda;

public sealed class ApiKitNotFoundException : Exception
{
    public ApiKitNotFoundException(string message) : base(message)
    {
    }
}
