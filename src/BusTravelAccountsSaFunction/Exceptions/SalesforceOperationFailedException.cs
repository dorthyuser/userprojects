namespace BusTravelAccountsSaFunction;

public sealed class SalesforceOperationFailedException(string statusCode, string description) : Exception(description)
{
    public string StatusCodeText { get; } = statusCode;

    public string Description { get; } = description;
}
