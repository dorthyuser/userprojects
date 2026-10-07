using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace BusTravelAccountsSaMainLambda;

public sealed class Function
{
    private static readonly JsonSerializerOptions JsonOptions = JsonHelper.CreateOptions();

    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        var settings = Settings.Load();
        var correlationId = CorrelationIdHelper.GetCorrelationId(request.Headers);
        var rawPath = request.RawPath ?? string.Empty;
        var path = PathHelper.StripBasePath(rawPath, settings.ListenerPath);
        var method = request.RequestContext?.Http?.Method?.ToUpperInvariant() ?? string.Empty;
        var contentType = HeaderHelper.GetHeaderValue(request.Headers, "content-type");
        var body = RequestBodyHelper.GetBody(request);

        try
        {
            if (PathHelper.IsAlive(path))
            {
                if (!HttpMethodHelper.IsGet(method)) return BuildMethodNotAllowed();
                return BuildTextResponse(200, "UP", correlationId);
            }

            if (PathHelper.IsReady(path))
            {
                if (!HttpMethodHelper.IsGet(method)) return BuildMethodNotAllowed();
                return BuildTextResponse(200, "UP", correlationId);
            }

            if (PathHelper.IsAccountsRoot(path))
            {
                if (HttpMethodHelper.IsGet(method))
                {
                    if (!ContentTypeHelper.IsSupportedForNoBody(contentType)) return BuildUnsupportedMediaType();
                    var service = new AccountsService(new AccountsDbClient(settings), JsonOptions);
                    var result = await service.GetAccountsAsync(request.QueryStringParameters, correlationId, CancellationToken.None);
                    return BuildJsonResponse(result.StatusCode, result.Body, correlationId);
                }

                if (HttpMethodHelper.IsPost(method))
                {
                    if (!ContentTypeHelper.IsJson(contentType)) return BuildUnsupportedMediaType();
                    var service = new AccountsService(new AccountsDbClient(settings), JsonOptions);
                    var result = await service.CreateAccountAsync(body, correlationId, CancellationToken.None);
                    return BuildJsonResponse(result.StatusCode, result.Body, correlationId);
                }

                return BuildMethodNotAllowed();
            }

            if (PathHelper.TryGetAccountId(path, out var id))
            {
                if (HttpMethodHelper.IsGet(method))
                {
                    if (!ContentTypeHelper.IsSupportedForNoBody(contentType)) return BuildUnsupportedMediaType();
                    var service = new AccountsService(new AccountsDbClient(settings), JsonOptions);
                    var result = await service.GetAccountByIdAsync(id, correlationId, CancellationToken.None);
                    return BuildJsonResponse(result.StatusCode, result.Body, correlationId);
                }

                if (HttpMethodHelper.IsPut(method))
                {
                    if (!ContentTypeHelper.IsJson(contentType)) return BuildUnsupportedMediaType();
                    var service = new AccountsService(new AccountsDbClient(settings), JsonOptions);
                    var result = await service.UpdateAccountAsync(id, body, correlationId, CancellationToken.None);
                    return BuildJsonResponse(result.StatusCode, result.Body, correlationId);
                }

                return BuildMethodNotAllowed();
            }

            return BuildNotFound();
        }
        catch (ApiKitBadRequestException ex)
        {
            context.Logger.LogLine($"{ex.GetType().Name}: {ex.Message}");
            return BuildJsonResponse(400, JsonSerializer.Serialize(ErrorResponseFactory.BadRequest(ex.Message), JsonOptions), correlationId);
        }
        catch (ApiKitNotFoundException ex)
        {
            context.Logger.LogLine($"{ex.GetType().Name}: {ex.Message}");
            return BuildJsonResponse(404, JsonSerializer.Serialize(ErrorResponseFactory.NotFound(ex.Message), JsonOptions), correlationId);
        }
        catch (ApiKitMethodNotAllowedException ex)
        {
            context.Logger.LogLine($"{ex.GetType().Name}: {ex.Message}");
            return BuildJsonResponse(405, JsonSerializer.Serialize(ErrorResponseFactory.MethodNotAllowed(ex.Message), JsonOptions), correlationId);
        }
        catch (ApiKitUnsupportedMediaTypeException ex)
        {
            context.Logger.LogLine($"{ex.GetType().Name}: {ex.Message}");
            return BuildJsonResponse(415, JsonSerializer.Serialize(ErrorResponseFactory.UnsupportedMediaType(ex.Message), JsonOptions), correlationId);
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"{ex.GetType().Name}: {ex.Message}");
            return BuildJsonResponse(500, JsonSerializer.Serialize(ErrorResponseFactory.InternalServerError(ex.Message), JsonOptions), correlationId);
        }
    }

    private static APIGatewayHttpApiV2ProxyResponse BuildTextResponse(int statusCode, string body, string correlationId) => new()
    {
        StatusCode = statusCode,
        Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["content-type"] = "text/plain; charset=utf-8",
            ["x-correlation-id"] = correlationId
        },
        Body = body,
        IsBase64Encoded = false
    };

    private static APIGatewayHttpApiV2ProxyResponse BuildJsonResponse(int statusCode, string body, string correlationId) => new()
    {
        StatusCode = statusCode,
        Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["content-type"] = "application/json; charset=utf-8",
            ["x-correlation-id"] = correlationId
        },
        Body = body,
        IsBase64Encoded = false
    };

    private static APIGatewayHttpApiV2ProxyResponse BuildNotFound() => BuildJsonResponse(404, JsonSerializer.Serialize(ErrorResponseFactory.NotFound("RESOURCE NOT FOUND"), JsonOptions), "");
    private static APIGatewayHttpApiV2ProxyResponse BuildMethodNotAllowed() => BuildJsonResponse(405, JsonSerializer.Serialize(ErrorResponseFactory.MethodNotAllowed("METHOD NOT ALLOWED"), JsonOptions), "");
    private static APIGatewayHttpApiV2ProxyResponse BuildUnsupportedMediaType() => BuildJsonResponse(415, JsonSerializer.Serialize(ErrorResponseFactory.UnsupportedMediaType("UNSUPPORTED MEDIA TYPE"), JsonOptions), "");
}
