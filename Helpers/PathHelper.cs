namespace BusTravelAccountsSaLambda;

public static class PathHelper
{
    public static string StripBasePath(string path, string basePath)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var normalizedBase = NormalizeBasePath(basePath);
        if (path.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
        {
            var remainder = path[normalizedBase.Length..];
            return string.IsNullOrEmpty(remainder) ? "/" : remainder.StartsWith('/') ? remainder : "/" + remainder;
        }

        return path;
    }

    public static bool IsAlive(string path) => path.Equals("/alive", StringComparison.OrdinalIgnoreCase);
    public static bool IsReady(string path) => path.Equals("/ready", StringComparison.OrdinalIgnoreCase);
    public static bool IsAccountsCollection(string path) => path.Equals("/accounts", StringComparison.OrdinalIgnoreCase);

    public static bool TryGetAccountId(string path, out string id)
    {
        id = string.Empty;
        if (!path.StartsWith("/accounts/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        id = path["/accounts/".Length..];
        return !string.IsNullOrWhiteSpace(id);
    }

    private static string NormalizeBasePath(string basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            return "/";
        }

        var trimmed = basePath.Trim();
        if (!trimmed.StartsWith('/'))
        {
            trimmed = "/" + trimmed;
        }

        if (trimmed.EndsWith("/*", StringComparison.Ordinal))
        {
            trimmed = trimmed[..^2];
        }

        return trimmed.EndsWith('/') ? trimmed[..^1] : trimmed;
    }
}
