using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using LambdacsharphttpLambda.Models;
using LambdacsharphttpLambda.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace LambdacsharphttpLambda;

public class Function
{
    private static readonly ServiceProvider ServiceProvider;

    static Function()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<SecretsHelper>();
        services.AddSingleton<ITravelcardDbConnection, TravelcardConnection>();
        services.AddSingleton<ITravelcardDbService, TravelcardService>();
        ServiceProvider = services.BuildServiceProvider();
    }

    public async Task<APIGatewayProxyResponse> lambdacsharphttp(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var logger = ServiceProvider.GetRequiredService<ILogger<Function>>();
        var clientId = GetHeaderValue(request.Headers, "client_id");
        logger.LogInformation("Controller entry: method={Method}, route={Route}, client_id={ClientId}", request.HttpMethod, request.Path, clientId);

        try
        {
            var validationError = ValidateRequest(request);
            if (validationError is not null)
            {
                logger.LogWarning("Validation failure: field={Field}, reason={Reason}", validationError.Field, validationError.Reason);
                return CreateErrorResponse((int)HttpStatusCode.BadRequest, "VALIDATION_ERROR", validationError.Reason);
            }

            var service = ServiceProvider.GetRequiredService<ITravelcardDbService>();
            var response = await service.ForwardAsync(request);
            logger.LogInformation("Response generated with status code {StatusCode}", response.StatusCode);
            return response;
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Configuration or dependency error in controller");
            return CreateErrorResponse((int)HttpStatusCode.InternalServerError, "CONFIGURATION_ERROR", "Service is not configured correctly.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in controller");
            return CreateErrorResponse((int)HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.");
        }
    }

    private static ValidationResult? ValidateRequest(APIGatewayProxyRequest request)
    {
        if (!string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
        {
            return new ValidationResult("httpMethod", "Only HTTP POST is supported.");
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return new ValidationResult("body", "Request body is required.");
        }

        return null;
    }

    private static APIGatewayProxyResponse CreateErrorResponse(int statusCode, string code, string message)
    {
        var body = System.Text.Json.JsonSerializer.Serialize(new Response
        {
            Success = false,
            Error = new ErrorResponse
            {
                Code = code,
                Message = message
            }
        });

        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Content-Type"] = "application/json"
            },
            Body = body
        };
    }

    private static string? GetHeaderValue(IDictionary<string, string>? headers, string headerName)
    {
        if (headers is null)
        {
            return null;
        }

        foreach (var item in headers)
        {
            if (string.Equals(item.Key, headerName, StringComparison.OrdinalIgnoreCase))
            {
                return item.Value;
            }
        }

        return null;
    }

    private sealed record ValidationResult(string Field, string Reason);
}
