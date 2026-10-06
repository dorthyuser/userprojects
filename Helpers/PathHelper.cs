using Microsoft.AspNetCore.Http;

namespace BusTravelAccountsEaMainFunction;

public static class PathHelper
{
    public static string NormalizeRelativePath(HttpRequest req, string listenerPath)
    {
        var path = req.Path.Value ?? string.Empty;
        var prefix = listenerPath.Trim();
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return path;
        }

        if (!prefix.StartsWith('/'))
        {
            prefix = "/" + prefix;
        }

        if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            var remainder = path[prefix.Length..];
            return string.IsNullOrEmpty(remainder) ? "/" : remainder.StartsWith('/') ? remainder : "/" + remainder;
        }

        return path;
    }
}