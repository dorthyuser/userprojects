using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Microsoft.Extensions.Logging;
using TestCsharpLambTc20260506Lambda.Models;
using TestCsharpLambTc20260506Lambda.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace TestCsharpLambTc20260506Lambda;

public class Function
{
    private readonly ILogger<Function> _logger;
    private readonly Service _service;

    public Function()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<Function>();
        _service = new Service(loggerFactory.CreateLogger<Service>());
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        _logger.LogInformation("Handler started");
        try
        {
            if (!request.Headers.TryGetValue("client_id", out var clientId) || string.IsNullOrWhiteSpace(clientId) || clientId.Length < 1 || clientId.Length > 128 || !System.Text.RegularExpressions.Regex.IsMatch(clientId, @"^[\w+]+$"))
                return ErrorResponse(HttpStatusCode.BadRequest, "INVALID_HEADER", "client_id is missing or invalid");

            if (request.Headers.TryGetValue("Content-Type", out var contentType) && !contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                return ErrorResponse(HttpStatusCode.BadRequest, "INVALID_HEADER", "Content-Type must contain application/json");

            if (request.Headers.TryGetValue("X-Correlation-Cust-Id", out var correlation) && (!string.IsNullOrWhiteSpace(correlation) && (correlation.Length > 100 || !System.Text.RegularExpressions.Regex.IsMatch(correlation, @"^[A-Za-z0-9_-]+$"))))
                return ErrorResponse(HttpStatusCode.BadRequest, "INVALID_HEADER", "X-Correlation-Cust-Id is invalid");

            if (string.IsNullOrWhiteSpace(request.Body))
                return ErrorResponse(HttpStatusCode.BadRequest, "INVALID_REQUEST", "Request body is required");

            var body = JsonSerializer.Deserialize<Request>(request.Body, JsonOptions()) ?? throw new InvalidOperationException("Invalid request body");
            var validation = _service.ValidateRequest(body);
            if (!validation.IsValid)
                return ErrorResponse(HttpStatusCode.BadRequest, "VALIDATION_ERROR", validation.ErrorMessage ?? "Validation failed");

            var response = await _service.CreateTravelcardAsync(body);
            _logger.LogInformation("Handler completed successfully");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
                Body = JsonSerializer.Serialize(response, JsonOptions())
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error");
            return ErrorResponse(HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred");
        }
    }

    private static APIGatewayProxyResponse ErrorResponse(HttpStatusCode statusCode, string code, string message)
        => new()
        {
            StatusCode = (int)statusCode,
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
            Body = JsonSerializer.Serialize(new { error = new { code, message } }, JsonOptions())
        };

    private static JsonSerializerOptions JsonOptions() => new()
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };
}