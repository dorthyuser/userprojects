namespace BusTravelAccountsSaMainLambda;

public static class PathHelper
{
    public static string StripBasePath(string rawPath, string basePath)
    {
        rawPath = Normalize(rawPath);
        basePath = Normalize(basePath);
        if (string.IsNullOrWhiteSpace(basePath) || basePath == "/") return rawPath;
        var prefix = basePath.TrimEnd('*').TrimEnd('/');
        if (rawPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            var remainder = rawPath[prefix.Length..];
            return string.IsNullOrWhiteSpace(remainder) ? "/" : remainder.StartsWith('/') ? remainder : "/" + remainder;
        }
        return rawPath;
    }

    public static bool IsAlive(string path) => string.Equals(Normalize(path), "/alive", StringComparison.OrdinalIgnoreCase);
    public static bool IsReady(string path) => string.Equals(Normalize(path), "/ready", StringComparison.OrdinalIgnoreCase);
    public static bool IsAccountsRoot(string path) => string.Equals(Normalize(path), "/accounts", StringComparison.OrdinalIgnoreCase);

    public static bool TryGetAccountId(string path, out string id)
    {
        id = string.Empty;
        path = Normalize(path);
        if (!path.StartsWith("/accounts/", StringComparison.OrdinalIgnoreCase)) return false;
        id = path.Substring("/accounts/".Length);
        return !string.IsNullOrWhiteSpace(id) && !id.Contains('/');
    }

    public static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "/";
        path = path.Replace('\\', '/');
        if (!path.StartsWith('/')) path = "/" + path;
        return path;
    }
}
