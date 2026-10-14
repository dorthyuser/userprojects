namespace BusTravelAccountsLambda;

/// <summary>
/// The Mule listener path (e.g. "api/v1/*", "/api/*", "/*" or empty) taken off the request path, so the
/// service routes on the remainder and works with and without the prefix.
/// </summary>
public static class BasePath
{
    /// <summary>
    /// "/api/v1/accounts" with "api/v1/*" → "/accounts"; the base path alone → "/"; a path outside the
    /// base path, or no base path at all, stays as it is. Whole segments only: "/apiv2" is not under "/api".
    /// </summary>
    public static string Strip(string? path, string? listenerPath)
    {
        var request = "/" + (path ?? string.Empty).Trim('/');
        var prefix = "/" + (listenerPath ?? string.Empty).Trim().TrimEnd('*').Trim('/');
        if (prefix == "/")
        {
            return request;
        }
        if (request.Equals(prefix, StringComparison.Ordinal))
        {
            return "/";
        }
        return request.StartsWith(prefix + "/", StringComparison.Ordinal) ? request[prefix.Length..] : request;
    }
}
