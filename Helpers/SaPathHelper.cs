namespace MuleaesaMainLambda;

public static class SaPathHelper
{
    public static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var value = path.Trim();
        if (!value.StartsWith('/'))
        {
            value = "/" + value;
        }

        if (value.Length > 1 && value.EndsWith('/'))
        {
            value = value.TrimEnd('/');
        }

        return value;
    }

    public static string StripBasePath(string requestPath, string basePath)
    {
        var normalizedRequest = NormalizePath(requestPath);
        var normalizedBase = NormalizePath(basePath);

        if (normalizedBase == "/")
        {
            return normalizedRequest;
        }

        if (normalizedRequest.Equals(normalizedBase, StringComparison.OrdinalIgnoreCase))
        {
            return "/";
        }

        if (normalizedRequest.StartsWith(normalizedBase + "/", StringComparison.OrdinalIgnoreCase))
        {
            return normalizedRequest[(normalizedBase.Length)..];
        }

        return normalizedRequest;
    }
}