namespace BusTravelAccountsSaLambda;

public sealed class ApiException : Exception
{
    public int StatusCode { get; }
    public string ErrorMessage { get; }
    public string ErrorDescription { get; }

    public ApiException(int statusCode, string errorMessage, string errorDescription) : base(errorDescription)
    {
        StatusCode = statusCode;
        ErrorMessage = errorMessage;
        ErrorDescription = errorDescription;
    }
}