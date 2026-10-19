using System.Text;

namespace BusTravelAccountsSaLambda;

public static class BodyHelper
{
    public static string DecodeBody(string? body, bool isBase64Encoded)
    {
        if (string.IsNullOrEmpty(body))
        {
            return string.Empty;
        }

        return isBase64Encoded ? Encoding.UTF8.GetString(Convert.FromBase64String(body)) : body;
    }
}