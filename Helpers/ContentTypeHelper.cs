namespace BusTravelAccountsSaLambda;

public static class ContentTypeHelper
{
    public static bool IsJson(string? contentType)
    {
        return !string.IsNullOrWhiteSpace(contentType) && contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }
}