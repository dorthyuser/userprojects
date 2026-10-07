namespace BusTravelAccountsSaMainLambda;

public static class CorrelationIdHelper
{
    public static string GetCorrelationId(IDictionary<string, string>? headers)
    {
        var correlationId = HeaderHelper.GetHeaderValue(headers, "x-correlation-id");
        if (!string.IsNullOrWhiteSpace(correlationId)) return correlationId.Trim();
        correlationId = HeaderHelper.GetHeaderValue(headers, "customer_correlation_id");
        if (!string.IsNullOrWhiteSpace(correlationId)) return correlationId.Trim();
        return Guid.NewGuid().ToString();
    }
}
