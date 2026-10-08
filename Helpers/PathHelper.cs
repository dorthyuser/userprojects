namespace BusTravelAccountsSaMainLambda;

public static class PathHelper
{
    public static string NormalizeIncomingPath(string path, string configuredBasePath)
    {
        var normalized = NormalizePath(path);
        var basePrefix = NormalizeBasePrefix(configuredBasePath);

        if (!string.IsNullOrEmpty(basePrefix) && normalized.StartsWith(basePrefix, StringComparison.OrdinalIgnoreCase))
        {
            var remainder = normalized.Substring(basePrefix.Length);
            if (string.IsNullOrEmpty(remainder))
            {
                return "/";
            }

            return remainder.StartsWith('/') ? remainder : "/" + remainder;
        }

        return normalized;
    }

    public static bool TryMatchAccountById(string path, out string id)
    {
        id = string.Empty;
        var normalized = NormalizePath(path);
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 2 && string.Equals(segments[0], "accounts", StringComparison.OrdinalIgnoreCase))
        {
            id = segments[1];
            return true;
        }

        return false;
    }

    public static bool IsAccountsPathPrefix(string path)
    {
        var normalized = NormalizePath(path);
        return normalized.StartsWith("/accounts", StringComparison.OrdinalIgnoreCase);
    }

    public static bool PathsEqual(string left, string right)
    {
        return string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeBasePrefix(string configuredBasePath)
    {
        var value = configuredBasePath.Trim();
        if (value.EndsWith("/*", StringComparison.Ordinal))
        {
            value = value[..^2];
        }

        if (!value.StartsWith('/'))
        {
            value = "/" + value;
        }

        return NormalizePath(value);
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var normalized = path.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        while (normalized.Contains("//", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("//", "/", StringComparison.Ordinal);
        }

        if (normalized.Length > 1 && normalized.EndsWith('/'))
        {
            normalized = normalized[..^1];
        }

        return normalized;
    }
}