namespace MuleaesaMainLambda;

public static class SaCorrelationHelper
{
    public static string GetCorrelationId(IDictionary<string, string>? headers)
    {
        var correlationId = SaHeaderHelper.GetHeader(headers, "X-Correlation-ID");
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.Trim();
        }

        correlationId = SaHeaderHelper.GetHeader(headers, "X_CORRELATION_ID");
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.Trim();
        }

        correlationId = SaHeaderHelper.GetHeader(headers, "CUSTOMER_CORRELATION_ID");
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.Trim();
        }

        return Guid.NewGuid().ToString();
    }
}