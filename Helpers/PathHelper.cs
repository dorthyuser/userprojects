namespace BusTravelAccountsLambda;

public static class PathHelper
{
    public static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "/";
        return path.StartsWith('/') ? path : "/" + path;
    }

    public static string StripBasePath(string path, string basePath)
    {
        path = Normalize(path);
        basePath = Normalize(basePath).TrimEnd('*');
        if (basePath.EndsWith('/')) basePath = basePath[..^1];
        if (path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            var remainder = path[basePath.Length..];
            return string.IsNullOrEmpty(remainder) ? "/" : remainder.StartsWith('/') ? remainder : "/" + remainder;
        }
        return path;
    }
}