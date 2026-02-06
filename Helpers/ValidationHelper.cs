using System;
using System.Collections.Generic;

namespace AgeApi.Helpers
{
    public static class ValidationHelper
    {
        public static IDictionary<string, string?> ParseQuery(string query)
        {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(query)) return result;

            var q = query;
            if (q.StartsWith("?")) q = q.Substring(1);

            var parts = q.Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var kv = part.Split('=', 2);
                var key = Uri.UnescapeDataString(kv[0]);
                var value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : string.Empty;
                result[key] = value;
            }
            return result;
        }
    }
}
