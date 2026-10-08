using System.Text.Json;
using Amazon.Lambda.Serialization.SystemTextJson;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace BusTravelAccountsSaMainLambda;

public sealed class Function
{
    private static readonly AccountsService AccountsService = new();
    private static readonly Settings Settings = Settings.Instance;

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            var path = request.Path ?? "/";
            var normalizedPath = PathHelper.NormalizeIncomingPath(path, Settings.HttpsListenerPath);
            var method = (request.HttpMethod ?? string.Empty).ToUpperInvariant();

            if (PathHelper.PathsEqual(normalizedPath, "/alive"))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                    Headers = ResponseHelper.CreatePlainTextHeaders(),
                    Body = "UP"
                };
            }

            if (PathHelper.PathsEqual(normalizedPath, "/ready"))
            {
                context.Logger.LogLine("INFO check-all-dependencies-are-alive");
                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                    Headers = ResponseHelper.CreatePlainTextHeaders(),
                    Body = "UP"
                };
            }

            if (PathHelper.PathsEqual(normalizedPath, "/accounts"))
            {
                if (method == "GET")
                {
                    var result = await AccountsService.GetAccountsAsync(request, context, CancellationToken.None);
                    return ResponseHelper.CreateJsonResponse(result.StatusCode, result.Body);
                }

                if (method == "POST")
                {
                    if (!MediaTypeHelper.IsJsonContentType(request.Headers))
                    {
                        return ResponseHelper.CreateJsonResponse(415, JsonSerializer.Serialize(ErrorFactory.Create(415, "UNSUPPORTED MEDIA TYPE", "Unsupported media type"), JsonOptionsHelper.Options));
                    }

                    var result = await AccountsService.CreateAccountAsync(request, context, CancellationToken.None);
                    return ResponseHelper.CreateJsonResponse(result.StatusCode, result.Body);
                }

                return ResponseHelper.CreateJsonResponse(405, JsonSerializer.Serialize(ErrorFactory.Create(405, "METHOD NOT ALLOWED", "Method not allowed"), JsonOptionsHelper.Options));
            }

            if (PathHelper.TryMatchAccountById(normalizedPath, out var id))
            {
                if (method == "GET")
                {
                    var result = await AccountsService.GetAccountByIdAsync(id, request, context, CancellationToken.None);
                    return ResponseHelper.CreateJsonResponse(result.StatusCode, result.Body);
                }

                if (method == "PUT")
                {
                    if (!MediaTypeHelper.IsJsonContentType(request.Headers))
                    {
                        return ResponseHelper.CreateJsonResponse(415, JsonSerializer.Serialize(ErrorFactory.Create(415, "UNSUPPORTED MEDIA TYPE", "Unsupported media type"), JsonOptionsHelper.Options));
                    }

                    var result = await AccountsService.UpdateAccountAsync(id, request, context, CancellationToken.None);
                    return ResponseHelper.CreateJsonResponse(result.StatusCode, result.Body);
                }

                return ResponseHelper.CreateJsonResponse(405, JsonSerializer.Serialize(ErrorFactory.Create(405, "METHOD NOT ALLOWED", "Method not allowed"), JsonOptionsHelper.Options));
            }

            return ResponseHelper.CreateJsonResponse(404, JsonSerializer.Serialize(ErrorFactory.Create(404, "RESOURCE NOT FOUND", "Resource not found"), JsonOptionsHelper.Options));
        }
        catch (JsonException ex)
        {
            context.Logger.LogLine("ERROR " + ex.GetType().Name + ": " + ex.Message);
            return ResponseHelper.CreateJsonResponse(400, JsonSerializer.Serialize(ErrorFactory.Create(400, "BAD REQUEST", ex.Message), JsonOptionsHelper.Options));
        }
        catch (FormatException ex)
        {
            context.Logger.LogLine("ERROR " + ex.GetType().Name + ": " + ex.Message);
            return ResponseHelper.CreateJsonResponse(400, JsonSerializer.Serialize(ErrorFactory.Create(400, "BAD REQUEST", ex.Message), JsonOptionsHelper.Options));
        }
        catch (DatabaseAuthenticationException ex)
        {
            context.Logger.LogLine("ERROR " + ex.GetType().Name + ": " + ex.Message);
            return ResponseHelper.CreateJsonResponse(401, JsonSerializer.Serialize(ErrorFactory.Create(401, "DATABASE CONNECTIVITY ERROR", ex.SafeDescription), JsonOptionsHelper.Options));
        }
        catch (DatabaseConnectivityException ex)
        {
            context.Logger.LogLine("ERROR " + ex.GetType().Name + ": " + ex.Message);
            return ResponseHelper.CreateJsonResponse(404, JsonSerializer.Serialize(ErrorFactory.Create(404, "DATABASE CONNECTIVITY ERROR", ex.SafeDescription), JsonOptionsHelper.Options));
        }
        catch (Exception ex)
        {
            context.Logger.LogLine("ERROR " + ex.GetType().Name + ": " + ex.Message);
            return ResponseHelper.CreateJsonResponse(500, JsonSerializer.Serialize(ErrorFactory.Create(500, "INTERNAL ERROR", "An unexpected error occurred"), JsonOptionsHelper.Options));
        }
    }
}