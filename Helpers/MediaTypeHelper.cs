namespace BusTravelAccountsSaMainLambda;

public static class MediaTypeHelper
{
    public static bool IsJsonContentType(IDictionary<string, string>? headers)
    {
        var contentType = HeaderHelper.GetHeaderValue(headers, "Content-Type");
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);
    }
}