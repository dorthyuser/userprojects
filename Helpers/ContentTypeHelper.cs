namespace BusTravelAccountsSaMainLambda;

public static class ContentTypeHelper
{
    public static bool IsJson(string? contentType) => !string.IsNullOrWhiteSpace(contentType) && contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    public static bool IsSupportedForNoBody(string? contentType) => string.IsNullOrWhiteSpace(contentType) || IsJson(contentType) || contentType.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) || contentType.Contains("multipart/form-data", StringComparison.OrdinalIgnoreCase);
}
