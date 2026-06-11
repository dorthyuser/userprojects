using System;

namespace life_time_calculator.Functions;

public static class QueryStringHelper
{
    public static string? GetQueryParameter(Uri uri, string key)
    {
        if (uri == null)
        {
            return null;
        }

        string query = uri.Query;
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        string[] parts = query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (string part in parts)
        {
            string[] pair = part.Split('=', 2);
            if (pair.Length == 2 && string.Equals(Uri.UnescapeDataString(pair[0]), key, StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(pair[1]);
            }
        }

        return null;
    }
}
