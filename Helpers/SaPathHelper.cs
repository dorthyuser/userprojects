namespace MulecombineMainLambda;

public static class SaPathHelper
{
    public static string NormalizeBasePath(string? configuredPath)
    {
        var path = configuredPath?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            path = "api/v1/*";
        }

        path = path.Trim();
        if (path.EndsWith("/*", StringComparison.Ordinal))
        {
            path = path[..^2];
        }

        return "/" + path.Trim('/');
    }

    public static string StripBasePath(string requestPath, string basePath)
    {
        if (requestPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            var remainder = requestPath[basePath.Length..];
            return string.IsNullOrEmpty(remainder) ? "/" : remainder.StartsWith('/') ? remainder : "/" + remainder;
        }

        return requestPath;
    }

    public static bool IsAlivePath(string path) => string.Equals(path, "/alive", StringComparison.OrdinalIgnoreCase);
    public static bool IsReadyPath(string path) => string.Equals(path, "/ready", StringComparison.OrdinalIgnoreCase);
}