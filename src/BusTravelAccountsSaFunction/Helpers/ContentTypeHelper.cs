namespace BusTravelAccountsSaFunction;

public static class ContentTypeHelper
{
    public static bool IsJson(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);
    }
}