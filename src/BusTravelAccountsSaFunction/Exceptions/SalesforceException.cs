using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BusTravelAccountsSaFunction;

public sealed class SalesforceException(string errorNamespace, string identifier, string description) : Exception(description)
{
    public string ErrorNamespace { get; } = errorNamespace;

    public string Identifier { get; } = identifier;

    public string Description { get; } = description;

    public static SalesforceException Of(int statusCode, string body, ILogger logger)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind == JsonValueKind.Array && document.RootElement.GetArrayLength() > 0)
            {
                var first = document.RootElement[0];
                var errorCode = first.TryGetProperty("errorCode", out var code) ? code.GetString() ?? string.Empty : string.Empty;
                var message = first.TryGetProperty("message", out var msg) ? msg.GetString() ?? string.Empty : string.Empty;
                return statusCode switch
                {
                    400 => new SalesforceException("SALESFORCE", "INVALID_INPUT", message),
                    403 when string.Equals(errorCode, "REQUEST_LIMIT_EXCEEDED", StringComparison.Ordinal) => new SalesforceException("SALESFORCE", "LIMIT_EXCEEDED", message),
                    404 => new SalesforceException("SALESFORCE", "NOT_FOUND", message),
                    _ => new SalesforceException("SALESFORCE", "CONNECTIVITY", message)
                };
            }
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse Salesforce error response");
        }

        return statusCode switch
        {
            400 => new SalesforceException("SALESFORCE", "INVALID_INPUT", "Salesforce rejected the request"),
            404 => new SalesforceException("SALESFORCE", "NOT_FOUND", "Salesforce resource not found"),
            _ => new SalesforceException("SALESFORCE", "CONNECTIVITY", "Failed to send request")
        };
    }
}
