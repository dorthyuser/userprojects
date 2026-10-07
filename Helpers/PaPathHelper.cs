namespace MulecombineMainLambda;

public static class PaPathHelper
{
    public static string NormalizeBasePath(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return string.Empty;
        }

        var path = configuredPath.Trim();
        if (!path.StartsWith("/", StringComparison.Ordinal))
        {
            path = "/" + path;
        }

        if (path.EndsWith("/*", StringComparison.Ordinal))
        {
            path = path[..^2];
        }

        if (path.Length > 1 && path.EndsWith("/", StringComparison.Ordinal))
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

        var normalizedRequest = requestPath.StartsWith("/", StringComparison.Ordinal) ? requestPath : "/" + requestPath;
        var normalizedBase = NormalizeBasePath(basePath);
        if (string.IsNullOrWhiteSpace(normalizedBase) || normalizedBase == "/")
        {
            return normalizedRequest;
        }

        if (normalizedRequest.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
        {
            var remainder = normalizedRequest[normalizedBase.Length..];
            return string.IsNullOrEmpty(remainder) ? "/" : remainder.StartsWith("/", StringComparison.Ordinal) ? remainder : "/" + remainder;
        }

        return normalizedRequest;
    }

    public static bool IsAlivePath(string path) => string.Equals(path, "/alive", StringComparison.OrdinalIgnoreCase);
    public static bool IsReadyPath(string path) => string.Equals(path, "/ready", StringComparison.OrdinalIgnoreCase);
}