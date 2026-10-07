namespace MulecombineMainLambda;

public static class CorrelationHelper
{
    public static string GetCorrelationId(IDictionary<string, string>? headers)
    {
        var correlationId = HeaderHelper.GetHeader(headers, "X-Correlation-ID");
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.Trim();
        }

        correlationId = HeaderHelper.GetHeader(headers, "x-correlation-id");
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.Trim();
        }

        return Guid.NewGuid().ToString();
    }
}