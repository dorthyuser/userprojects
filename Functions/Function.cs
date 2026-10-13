using System.Text;
using System.Text.Json;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace BusTravelAccountsSaLambda;

public sealed class Function
{
    private static readonly Settings Settings = new();
    private static readonly AccountsService Service = new(Settings);

    /// <summary>
    /// Lambda entry point: makes the header names case-insensitive (clients and HTTP/2 send them in any case),
    /// connects AppLog to this invocation and logs every answered request once; HandleRequestAsync answers it.
    /// </summary>
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        request.Headers = new Dictionary<string, string>(request.Headers ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
        AppLog.Use(context.Logger);
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            var response = await HandleRequestAsync(request, context);
            AppLog.Request(request, response.StatusCode, System.Diagnostics.Stopwatch.GetElapsedTime(started));
            return response;
        }
        catch (Exception ex)
        {
            AppLog.Error($"{request.HttpMethod} {request.Path} failed", ex);
            throw;
        }
    }

    private async Task<APIGatewayProxyResponse> HandleRequestAsync(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var path = request.Path ?? string.Empty;
        var method = request.HttpMethod ?? string.Empty;
        var headers = request.Headers;
        var query = request.QueryStringParameters;
        var body = request.Body;
        if (request.IsBase64Encoded && !string.IsNullOrEmpty(body))
        {
            body = Encoding.UTF8.GetString(Convert.FromBase64String(body));
        }

        var result = await Service.HandleAsync(method, path, query, headers, body, CancellationToken.None);
        return new APIGatewayProxyResponse
        {
            StatusCode = result.StatusCode,
            Headers = result.Headers,
            Body = result.Body
        };
    }
}
