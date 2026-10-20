namespace BusTravelAccountsLambda;

public static class BasePathHelper
{
    public static string Strip(string path, string basePath)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var normalizedPath = path.StartsWith('/') ? path : "/" + path;
        var normalizedBase = basePath.Trim();
        if (normalizedBase.EndsWith("/*", StringComparison.Ordinal))
        {
            normalizedBase = normalizedBase[..^2];
        }
        if (!normalizedBase.StartsWith('/'))
        {
            normalizedBase = "/" + normalizedBase;
        }
        if (normalizedPath.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
        {
            var remainder = normalizedPath[normalizedBase.Length..];
            return string.IsNullOrEmpty(remainder) ? "/" : remainder;
        }
        return normalizedPath;
    }
}
