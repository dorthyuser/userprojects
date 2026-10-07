using System.Text;
namespace BusTravelAccountsSaMainLambda;

public static class RequestBodyHelper
{
    public static string GetBody(APIGatewayProxyRequest request)
    {
        if (string.IsNullOrEmpty(request.Body)) return string.Empty;
        if (!request.IsBase64Encoded) return request.Body;
        var bytes = Convert.FromBase64String(request.Body);
        return Encoding.UTF8.GetString(bytes);
    }
}
