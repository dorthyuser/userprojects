namespace BusTravelAccountsSaMainLambda;

public static class HeaderHelper
{
    public static string? GetHeaderValue(IDictionary<string, string>? headers, string name)
    {
        if (headers is null) return null;
        foreach (var pair in headers)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase)) return pair.Value;
        }
        return null;
    }

    public static string? GetQueryValue(IDictionary<string, string>? query, string name)
    {
        if (query is null) return null;
        foreach (var pair in query)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase)) return pair.Value;
        }
        return null;
    }
}
