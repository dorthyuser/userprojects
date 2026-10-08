using System.Text;

namespace BusTravelAccountsSaMainLambda;

public static class BodyHelper
{
    public static string GetBodyText(APIGatewayProxyRequest request)
    {
        var body = request.Body ?? string.Empty;
        if (!request.IsBase64Encoded)
        {
            return body;
        }

        var bytes = Convert.FromBase64String(body);
        return Encoding.UTF8.GetString(bytes);
    }
}