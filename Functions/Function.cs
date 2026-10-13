using System.Text;
using System.Text.Json;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace BusTravelAccountsSaLambda;

public sealed class Function
{
    private static readonly Settings Settings = Settings.Load();
    private static readonly AccountsService AccountsService = new(Settings);
    private static readonly HealthService HealthService = new(Settings);

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
            var method = request.HttpMethod ?? string.Empty;
            var normalizedPath = PathHelper.StripBasePath(path, Settings.BasePath);

            if (PathHelper.IsAlive(normalizedPath))
            {
                return await HealthService.HandleAliveAsync(method);
            }

            if (PathHelper.IsReady(normalizedPath))
            {
                return await HealthService.HandleReadyAsync(method);
            }

            if (PathHelper.IsAccountsCollection(normalizedPath))
            {
                if (method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                {
                    return await AccountsService.GetAccountsAsync(request.QueryStringParameters);
                }

                if (method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                {
                    return await AccountsService.CreateAccountAsync(request.Headers, request.Body, request.IsBase64Encoded);
                }

                return ResponseFactory.MethodNotAllowed();
            }

            if (PathHelper.TryGetAccountId(normalizedPath, out var id))
            {
                if (method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                {
                    return await AccountsService.GetAccountByIdAsync(id);
                }

                if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase))
                {
                    return await AccountsService.UpdateAccountAsync(id, request.Headers, request.Body, request.IsBase64Encoded);
                }

                return ResponseFactory.MethodNotAllowed();
            }

            return ResponseFactory.NotFound();
        }
        catch (Exception ex)
        {
            context.Logger.LogLine(ex.ToString());
            return ResponseFactory.InternalServerError();
        }
    }
}
