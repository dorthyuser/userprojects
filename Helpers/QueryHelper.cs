namespace BusTravelAccountsSaMainLambda;

public static class QueryHelper
{
    public static string? GetQueryValue(IDictionary<string, string>? query, string name)
    {
        if (query is null)
        {
            return null;
        }

        foreach (var item in query)
        {
            if (string.Equals(item.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return item.Value;
            }
        }

        return null;
    }
}