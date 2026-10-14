using System.Collections.Generic;

namespace BusTravelAccountsLambda;

public static class ResponseHelper
{
    public static IDictionary<string, string> JsonHeaders() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Content-Type"] = "application/json"
    };

    public static IDictionary<string, string> TextHeaders() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Content-Type"] = "text/plain"
    };
}