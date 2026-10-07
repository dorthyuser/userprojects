namespace MulecombineMainLambda;

public static class PathHelper
{
    public static string NormalizeBasePath(string? configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath) ? "api/v1/*" : configuredPath.Trim();
        if (!path.StartsWith("/", StringComparison.Ordinal))
        {
            path = "/" + path;
        }

        if (path.EndsWith("/*", StringComparison.Ordinal))
        {
            path = path[..^2];
        }

        if (path.EndsWith("/", StringComparison.Ordinal) && path.Length > 1)
        {
            path = path[..^1];
        }

        return path;
    }

    public static string StripBasePath(string requestPath, string basePath)
    {
        if (string.IsNullOrWhiteSpace(requestPath))
        {
            return "/";
        }

        var normalizedRequestPath = requestPath.StartsWith("/", StringComparison.Ordinal) ? requestPath : "/" + requestPath;
        var normalizedBasePath = NormalizeBasePath(basePath);

        if (normalizedRequestPath.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase))
        {
            var remainder = normalizedRequestPath[normalizedBasePath.Length..];
            return string.IsNullOrEmpty(remainder) ? "/" : remainder;
        }

        return normalizedRequestPath;
    }

    public static bool IsAlivePath(string path) => path.Equals("/alive", StringComparison.OrdinalIgnoreCase);

    public static bool IsReadyPath(string path) => path.Equals("/ready", StringComparison.OrdinalIgnoreCase);
}