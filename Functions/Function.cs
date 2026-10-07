using System.Text;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace MuleaesaMainLambda;

public sealed class Function
{
    private static readonly EaApiService ApiService = new();

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            var headers = request.Headers;
            var correlationId = CorrelationHelper.GetCorrelationId(headers);
            var basePath = EaSettings.Current().ListenerPath;
            var requestPath = request.Path ?? string.Empty;
            var relativePath = PathHelper.StripBasePath(requestPath, basePath);
            var method = request.HttpMethod?.ToUpperInvariant() ?? string.Empty;
            var body = RequestBodyHelper.GetBody(request);

            if (relativePath == "/alive" && method == "GET")
            {
                var result = await ApiService.GetAliveAsync().ConfigureAwait(false);
                return ResponseHelper.ToResponse(result, correlationId);
            }

            if (relativePath == "/ready" && method == "GET")
            {
                var result = await ApiService.GetReadyAsync().ConfigureAwait(false);
                return ResponseHelper.ToResponse(result, correlationId);
            }

            if (relativePath == "/accounts" && method == "GET")
            {
                if (!RequestBodyHelper.IsSupportedGetRequest(request))
                {
                    throw new EaUnsupportedMediaTypeException("UNSUPPORTED MEDIA TYPE");
                }

                var result = await ApiService.GetAccountsAsync(request.QueryStringParameters, headers).ConfigureAwait(false);
                return ResponseHelper.ToResponse(result, correlationId);
            }

            if (relativePath == "/accounts" && method == "POST")
            {
                if (!RequestBodyHelper.IsJsonContentType(headers))
                {
                    throw new EaUnsupportedMediaTypeException("UNSUPPORTED MEDIA TYPE");
                }

                var result = await ApiService.CreateAccountAsync(body, headers).ConfigureAwait(false);
                return ResponseHelper.ToResponse(result, correlationId);
            }

            if (PathHelper.TryGetAccountId(relativePath, out var id))
            {
                if (method == "GET")
                {
                    var result = await ApiService.GetAccountByIdAsync(id, request.QueryStringParameters, headers).ConfigureAwait(false);
                    return ResponseHelper.ToResponse(result, correlationId);
                }

                if (method == "PUT")
                {
                    if (!RequestBodyHelper.IsJsonContentType(headers))
                    {
                        throw new EaUnsupportedMediaTypeException("UNSUPPORTED MEDIA TYPE");
                    }

                    var result = await ApiService.UpdateAccountAsync(id, body, headers).ConfigureAwait(false);
                    return ResponseHelper.ToResponse(result, correlationId);
                }

                throw new EaMethodNotAllowedException("METHOD NOT ALLOWED");
            }

            if (relativePath == "/accounts")
            {
                throw new EaMethodNotAllowedException("METHOD NOT ALLOWED");
            }

            throw new EaNotFoundException("RESOURCE NOT FOUND");
        }
        catch (EaBadRequestException ex)
        {
            context.Logger.LogLine($"{ex.GetType().Name}: {ex.Message}");
            return ResponseHelper.ToErrorResponse(400, "BAD REQUEST", ex.Message);
        }
        catch (EaNotFoundException ex)
        {
            context.Logger.LogLine($"{ex.GetType().Name}: {ex.Message}");
            return ResponseHelper.ToErrorResponse(404, "RESOURCE NOT FOUND", ex.Message);
        }
        catch (EaMethodNotAllowedException ex)
        {
            context.Logger.LogLine($"{ex.GetType().Name}: {ex.Message}");
            return ResponseHelper.ToErrorResponse(405, "METHOD NOT ALLOWED", ex.Message);
        }
        catch (EaUnsupportedMediaTypeException ex)
        {
            context.Logger.LogLine($"{ex.GetType().Name}: {ex.Message}");
            return ResponseHelper.ToErrorResponse(415, "UNSUPPORTED MEDIA TYPE", ex.Message);
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"{ex.GetType().Name}: {ex.Message}");
            return ResponseHelper.ToErrorResponse(500, "EA INTERNAL ERROR", "Unexpected error");
        }
    }
}
