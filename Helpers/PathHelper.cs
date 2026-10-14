namespace BusTravelAccountsLambda;

public static class PathHelper
{
    public static string StripBasePath(string path, string basePath)
    {
        var normalizedBase = basePath.TrimEnd('*').TrimEnd('/');
        if (string.IsNullOrEmpty(normalizedBase)) return path;
        if (path.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
        {
            var remainder = path[normalizedBase.Length..];
            return string.IsNullOrEmpty(remainder) ? "/" : remainder.StartsWith('/') ? remainder : "/" + remainder;
        }
        return path;
    }
}