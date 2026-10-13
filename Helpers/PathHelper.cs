namespace BusTravelAccountsSaLambda;

public static class PathHelper
{
    public static string NormalizeBasePath(string basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            return string.Empty;
        }

        var trimmed = basePath.Trim();
        if (trimmed.EndsWith("/*", StringComparison.Ordinal))
        {
            trimmed = trimmed[..^2];
        }

        if (!trimmed.StartsWith('/'))
        {
            trimmed = "/" + trimmed;
        }

        return trimmed.TrimEnd('/');
    }

    public static string StripBasePath(string path, string basePath)
    {
        var normalizedBase = NormalizeBasePath(basePath);
        if (string.IsNullOrEmpty(normalizedBase) || !path.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        var remainder = path[normalizedBase.Length..];
        return string.IsNullOrEmpty(remainder) ? "/" : remainder;
    }
}