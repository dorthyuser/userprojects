namespace BusTravelAccountsSaMainLambda;

public static class HttpMethodHelper
{
    public static bool IsGet(string method) => string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase);
    public static bool IsPost(string method) => string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase);
    public static bool IsPut(string method) => string.Equals(method, "PUT", StringComparison.OrdinalIgnoreCase);
}
