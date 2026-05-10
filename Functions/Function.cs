using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Dashboard2Lambda.Models;
using Dashboard2Lambda.Services;
using Microsoft.Extensions.Logging;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Dashboard2Lambda;

public class Function
{
    private readonly Service _service;
    private readonly ILogger<Function> _logger;

    public Function() : this(new Service(), LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Function>())
    {
    }

    public Function(Service service, ILogger<Function> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task<APIGatewayProxyResponse> dashboard2(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var clientId = GetHeaderValue(request.Headers, "client_id") ?? GetHeaderValue(request.Headers, "client-id") ?? string.Empty;
        _logger.LogInformation("HTTP method: {Method}, route: {Route}, client_id: {ClientId}", request.HttpMethod, request.Path, clientId);

        try
        {
            _logger.LogInformation("Validating request...");
            var validationError = ValidateRequest(request);
            if (validationError is not null)
            {
                _logger.LogWarning("Validation failed: {Reason}", validationError.ErrorMessage);
                return CreateResponse(HttpStatusCode.BadRequest, validationError);
            }

            _logger.LogInformation("Validation passed.");

            var model = JsonSerializer.Deserialize<Request>(request.Body ?? string.Empty, JsonOptions());
            if (model is null)
            {
                var error = Response.Fail("Invalid request body.");
                return CreateResponse(HttpStatusCode.BadRequest, error);
            }

            var inputValidation = ValidateModel(model);
            if (inputValidation is not null)
            {
                _logger.LogWarning("Validation failed: {Reason}", inputValidation.ErrorMessage);
                return CreateResponse(HttpStatusCode.BadRequest, inputValidation);
            }

            var result = await _service.CreateAsync(model);
            _logger.LogInformation("Response generated id: {GeneratedId}", result.GeneratedId);
            return CreateResponse(HttpStatusCode.OK, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in dashboard2 handler");
            return CreateResponse(HttpStatusCode.InternalServerError, Response.Fail("An unexpected error occurred."));
        }
    }

    private static Response? ValidateRequest(APIGatewayProxyRequest request)
    {
        return null;
    }

    private static Response? ValidateModel(Request model)
    {
        if (string.IsNullOrWhiteSpace(model.ClientId)) return Response.Fail("Field 'client_id' is required.");
        if (string.IsNullOrWhiteSpace(model.Name)) return Response.Fail("Field 'name' is required.");
        if (model.Status is null) return Response.Fail($"Invalid value for field 'status'. Accepted values: {string.Join(", ", Enum.GetNames(typeof(RecordStatus)))}");
        return null;
    }

    private static string? GetHeaderValue(IDictionary<string, string>? headers, string key)
    {
        if (headers is null) return null;
        foreach (var kvp in headers)
        {
            if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase)) return kvp.Value;
        }
        return null;
    }

    private static APIGatewayProxyResponse CreateResponse(HttpStatusCode statusCode, Response response)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            },
            Body = JsonSerializer.Serialize(response, JsonOptions())
        };
    }

    private static JsonSerializerOptions JsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(null, allowIntegerValues: false));
        return options;
    }
}
