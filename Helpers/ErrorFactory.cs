namespace BusTravelAccountsSaMainLambda;

public static class ErrorFactory
{
    public static ErrorEnvelope Create(int statusCode, string message, string description)
    {
        return new ErrorEnvelope
        {
            Error = new ErrorDetail
            {
                ErrorCode = statusCode,
                ErrorDateTime = DateTimeOffset.UtcNow,
                ErrorMessage = message,
                ErrorDescription = description
            }
        };
    }
}