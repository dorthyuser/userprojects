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
        try
        {
            var path = request.Path ?? string.Empty;
            var method = request.HttpMethod?.ToUpperInvariant() ?? string.Empty;
            var strippedPath = BasePath.Strip(path, Settings.ListenerPath);
            var contentType = HeaderHelper.GetHeader(request.Headers, "Content-Type");

            if (strippedPath == "/alive")
            {
                return ResponseHelper.Text(200, "UP");
            }

            if (strippedPath == "/ready")
            {
                var ready = await Service.ReadyAsync();
                return ResponseHelper.Text(ready.StatusCode, ready.Body ?? "UP");
            }

            if (strippedPath == "/accounts")
            {
                if (method == "GET")
                {
                    return await Service.GetAccountsAsync(request.QueryStringParameters);
                }

                if (method == "POST")
                {
                    if (!ContentTypeHelper.IsJson(contentType))
                    {
                        return ErrorResponses.UnsupportedMediaType();
                    }

                    return await Service.PostAccountAsync(request.Body, request.IsBase64Encoded);
                }

                return ErrorResponses.MethodNotAllowed();
            }

            if (PathHelper.IsAccountById(strippedPath, out var id))
            {
                if (method == "GET")
                {
                    return await Service.GetAccountByIdAsync(id);
                }

                if (method == "PUT")
                {
                    if (!ContentTypeHelper.IsJson(contentType))
                    {
                        return ErrorResponses.UnsupportedMediaType();
                    }

                    return await Service.PutAccountAsync(id, request.Body, request.IsBase64Encoded);
                }

                return ErrorResponses.MethodNotAllowed();
            }

            return ErrorResponses.NotFound();
        }
        catch (Exception ex)
        {
            AppLog.Error("Unhandled exception", ex);
            return ErrorResponses.InternalServerError();
        }
    }
}
