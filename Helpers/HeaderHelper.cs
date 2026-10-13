namespace BusTravelAccountsSaLambda;

public static class HeaderHelper
{
    public static string? GetHeader(IDictionary<string, string>? headers, string name)
    {
        if (headers is null)
        {
            return null;
        }

        return headers.TryGetValue(name, out var value) ? value : headers.FirstOrDefault(kvp => string.Equals(kvp.Key, name, StringComparison.OrdinalIgnoreCase)).Value;
    }
}