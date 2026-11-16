namespace BusTravelAccountsSaFunction;

public static class RequestContentTypeHelper
{
    public static bool IsJson(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        var mediaType = contentType.Split(';', 2, StringSplitOptions.TrimEntries)[0];
        return string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase);
    }
}
