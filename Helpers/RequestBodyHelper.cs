using System.Text;

namespace BusTravelAccountsSaLambda;

public static class RequestBodyHelper
{
    public static T Parse<T>(string? body, bool isBase64Encoded)
    {
        var text = body ?? string.Empty;
        if (isBase64Encoded && !string.IsNullOrEmpty(text))
        {
            text = Encoding.UTF8.GetString(Convert.FromBase64String(text));
        }

        return JsonHelper.Deserialize<T>(text) ?? throw new InvalidOperationException("Invalid request body");
    }
}
