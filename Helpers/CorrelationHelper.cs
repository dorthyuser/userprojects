namespace BusTravelAccountsLambda;

public static class CorrelationHelper
{
    public static string GetCorrelationId(IDictionary<string, string>? headers)
    {
        var value = HeaderHelper.Get(headers, "X-Correlation-ID");
        return string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString() : value.Trim();
    }
}