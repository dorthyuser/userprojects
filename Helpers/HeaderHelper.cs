namespace BusTravelAccountsSaLambda;

public static class HeaderHelper
{
    public static string? GetValue(IDictionary<string, string>? headers, string name)
    {
        if (headers is null)
        {
            return null;
        }

        return headers.TryGetValue(name, out var value) ? value : headers.FirstOrDefault(kvp => kvp.Key.Equals(name, StringComparison.OrdinalIgnoreCase)).Value;
    }
}
