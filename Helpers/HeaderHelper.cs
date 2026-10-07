namespace MulecombineMainLambda;

public static class HeaderHelper
{
    public static string? GetHeader(IDictionary<string, string>? headers, string name)
    {
        if (headers is null)
        {
            return null;
        }

        foreach (var header in headers)
        {
            if (string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return header.Value;
            }
        }

        return null;
    }
}