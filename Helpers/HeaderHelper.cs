namespace BusTravelAccountsSaLambda;

public static class HeaderHelper
{
    public static string? GetHeader(IDictionary<string, string>? headers, string name)
    {
        if (headers is null)
        {
            return null;
        }

        foreach (var pair in headers)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value;
            }
        }

        return null;
    }

    public static string? GetQuery(IDictionary<string, string>? query, string name)
    {
        if (query is null)
        {
            return null;
        }

        foreach (var pair in query)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value;
            }
        }

        return null;
    }
}