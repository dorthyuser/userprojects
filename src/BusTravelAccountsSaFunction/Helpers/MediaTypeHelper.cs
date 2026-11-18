namespace BusTravelAccountsSaFunction;

public static class MediaTypeHelper
{
    public static bool IsJson(string? contentType) =>
        !string.IsNullOrWhiteSpace(contentType) &&
        contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);
}