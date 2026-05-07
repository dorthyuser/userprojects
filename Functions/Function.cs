using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Microsoft.Extensions.Logging;
using Travelcardchsarplambda1050Lambda.Models;
using Travelcardchsarplambda1050Lambda.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Travelcardchsarplambda1050Lambda;

public class Function
{
    private readonly ILogger<Function> _logger;
    private readonly Service _service;

    public Function()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<Function>();
        _service = new Service(_logger);
    }

    public async Task<APIGatewayProxyResponse> travelcardchsarplambda1050(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var clientId = GetHeader(request, "client_id");
        _logger.LogInformation("Controller entry: {Method} {Path} client_id={ClientId}", request.HttpMethod, request.Path, clientId);

        try
        {
            _logger.LogInformation("Validating request...");
            var validationError = RequestValidator.Validate(request);
            if (validationError is not null)
            {
                _logger.LogWarning("Validation failure: {Field} - {Reason}", validationError.Field, validationError.Reason);
                return BuildResponse(HttpStatusCode.BadRequest, new ErrorResponse("VALIDATION_ERROR", validationError.Message));
            }
            _logger.LogInformation("Validation passed.");

            var parsed = JsonSerializer.Deserialize<CreateTravelcardRequest>(request.Body ?? "{}", JsonOptionsFactory.CreateOptions())
                         ?? throw new InvalidOperationException("Invalid request body.");

            var result = await _service.CreateTravelcardAsync(parsed, clientId, context.AwsRequestId);
            _logger.LogInformation("Response generated ID: {Id}", result.TravelcardId);
            return BuildResponse(HttpStatusCode.Created, result);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Request body deserialization error in controller.");
            return BuildResponse(HttpStatusCode.BadRequest, new ErrorResponse("INVALID_JSON", "Malformed JSON request body."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in controller.");
            return BuildResponse(HttpStatusCode.InternalServerError, new ErrorResponse("INTERNAL_ERROR", "An unexpected error occurred."));
        }
    }

    private static string? GetHeader(APIGatewayProxyRequest request, string key)
        => request.Headers != null && request.Headers.TryGetValue(key, out var value) ? value : null;

    private static APIGatewayProxyResponse BuildResponse(HttpStatusCode statusCode, object body)
        => new()
        {
            StatusCode = (int)statusCode,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            },
            Body = JsonSerializer.Serialize(body, JsonOptionsFactory.CreateOptions())
        };
}