using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using DemoTravelcardClincalLambda.Models;
using DemoTravelcardClincalLambda.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace DemoTravelcardClincalLambda;

public class Function
{
    private readonly ILogger<Function> _logger;
    private readonly Service _service;

    public Function() : this(LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Function>())
    {
    }

    public Function(ILogger<Function> logger)
    {
        _logger = logger;
        _service = new Service(logger);
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var clientId = HeaderHelper.GetHeaderValue(request.Headers, "client_id");
        _logger.LogInformation("HTTP method: {Method}, route: {Route}, client_id: {ClientId}", request.HttpMethod, request.Path, clientId);

        try
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return ErrorResponse(HttpStatusCode.BadRequest, "Missing required header 'client_id'.");
            }

            if (string.IsNullOrWhiteSpace(request.Body))
            {
                return ErrorResponse(HttpStatusCode.BadRequest, "Request body is required.");
            }

            var model = JsonHelper.Deserialize<Request>(request.Body);
            var validationError = RequestValidator.Validate(model);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                return ErrorResponse(HttpStatusCode.BadRequest, validationError);
            }

            var created = await _service.CreateTravelcardAsync(model);
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
                Body = JsonHelper.Serialize(created)
            };
        }
        catch (AppException ex)
        {
            _logger.LogError(ex, "Application error");
            return ErrorResponse(ex.StatusCode, ex.Message);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON payload");
            return ErrorResponse(HttpStatusCode.BadRequest, "Invalid JSON payload.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            return ErrorResponse(HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static APIGatewayProxyResponse ErrorResponse(HttpStatusCode statusCode, string message)
        => new()
        {
            StatusCode = (int)statusCode,
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            Body = JsonHelper.Serialize(new ErrorResponseModel(message))
        };
}

public static class HeaderHelper
{
    public static string? GetHeaderValue(IDictionary<string, string>? headers, string key)
    {
        if (headers is null) return null;
        return headers.FirstOrDefault(h => string.Equals(h.Key, key, StringComparison.OrdinalIgnoreCase)).Value;
    }
}

public static class JsonHelper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter(null, allowIntegerValues: false) },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options)!;
    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
}

public sealed class AppException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public AppException(HttpStatusCode statusCode, string message) : base(message) => StatusCode = statusCode;
}