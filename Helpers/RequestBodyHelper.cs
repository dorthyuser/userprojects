using System.Text;

namespace MuleaesaMainLambda;

public static class RequestBodyHelper
{
    public static string? GetBody(APIGatewayProxyRequest request)
    {
        if (request.Body is null)
        {
            return null;
        }

        if (!request.IsBase64Encoded)
        {
            return request.Body;
        }

        var bytes = Convert.FromBase64String(request.Body);
        return Encoding.UTF8.GetString(bytes);
    }

    public static bool IsJsonContentType(IDictionary<string, string>? headers)
    {
        var contentType = HeaderHelper.GetHeader(headers, "Content-Type");
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSupportedGetRequest(APIGatewayProxyRequest request)
    {
        return true;
    }
}
