namespace BusTravelAccountsSaLambda;

public static class PathHelper
{
    public static string StripBasePath(string path, string listenerPath)
    {
        var normalizedListener = NormalizeListenerPath(listenerPath);
        if (path.StartsWith(normalizedListener, StringComparison.OrdinalIgnoreCase))
        {
            var remainder = path[normalizedListener.Length..];
            return string.IsNullOrEmpty(remainder) ? "/" : remainder;
        }

        return path;
    }

    private static string NormalizeListenerPath(string listenerPath)
    {
        var value = listenerPath.Trim();
        if (value.EndsWith("/*", StringComparison.Ordinal))
        {
            value = value[..^2];
        }

        if (!value.StartsWith('/'))
        {
            value = "/" + value;
        }

        return value.TrimEnd('/');
    }
}