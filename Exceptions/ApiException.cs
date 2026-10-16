namespace BusTravelAccountsLambda;

public sealed class ApiException : Exception
{
    public int StatusCode { get; }
    public string Body { get; }

    public ApiException(int statusCode, string body) : base(body)
    {
        StatusCode = statusCode;
        Body = body;
    }
}