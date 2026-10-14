namespace BusTravelAccountsLambda;

public static class HeaderHelper
{
    public static string? Get(IDictionary<string, string>? headers, string name)
    {
        if (headers == null) return null;
        foreach (var kv in headers)
        {
            if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase)) return kv.Value;
        }
        return null;
    }
}