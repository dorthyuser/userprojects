namespace MuleaesaMainLambda;

public static class PathHelper
{
    public static string NormalizePath(string? path)
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

        if (normalized.Length > 1 && normalized.EndsWith('/'))
        {
            normalized = normalized.TrimEnd('/');
        }

        return normalized;
    }

    public static string StripBasePath(string requestPath, string basePath)
    {
        var normalizedRequestPath = NormalizePath(requestPath);
        var normalizedBasePath = NormalizePath(basePath).TrimEnd('*');
        normalizedBasePath = NormalizePath(normalizedBasePath);

        if (normalizedBasePath == "/")
        {
            return normalizedRequestPath;
        }

        if (normalizedRequestPath.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase))
        {
            var remainder = normalizedRequestPath[(normalizedBasePath.Length)..];
            return string.IsNullOrEmpty(remainder) ? "/" : NormalizePath(remainder);
        }

        return normalizedRequestPath;
    }

    public static bool TryGetAccountId(string relativePath, out string id)
    {
        id = string.Empty;
        var normalized = NormalizePath(relativePath);
        if (!normalized.StartsWith("/accounts/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        id = normalized[("/accounts/".Length)..];
        return !string.IsNullOrWhiteSpace(id) && !id.Contains('/');
    }
}
