using System.Globalization;
using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Travelcardcsharplambda349Lambda.Models;
using Travelcardcsharplambda349Lambda.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Travelcardcsharplambda349Lambda;

public class Function
{
    private readonly Service _service;

    public Function()
    {
        _service = new Service(new ConsoleLogger());
    }

    public async Task<APIGatewayProxyResponse> travelcardcsharplambda349(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var headers = new Dictionary<string, string>(request.Headers ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
        try
        {
            _service.LogInfo("Handler started.");

            if (!headers.TryGetValue("client_id", out var clientId) || string.IsNullOrWhiteSpace(clientId))
                return ErrorResponse(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "client_id is required.");

            if (clientId.Length < 1 || clientId.Length > 128)
                return ErrorResponse(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "client_id length must be between 1 and 128.");

            if (!headers.TryGetValue("Content-Type", out var contentType) || !contentType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
                return ErrorResponse(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "Content-Type must be application/json.");

            if (string.IsNullOrWhiteSpace(request.Body))
                return ErrorResponse(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "Request body is required.");

            var model = JsonSerializer.Deserialize<Request>(request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (model is null)
                return ErrorResponse(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "Invalid request body.");

            var validationError = _service.Validate(model);
            if (validationError is not null)
                return ErrorResponse(HttpStatusCode.BadRequest, "VALIDATION_ERROR", validationError);

            var response = await _service.CreateAsync(model, clientId, headers.TryGetValue("X-Correlation-Cust-Id", out var corr) ? corr : null);
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Body = JsonSerializer.Serialize(response),
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }
            };
        }
        catch (Exception ex)
        {
            _service.LogError("Unhandled error.", ex);
            return ErrorResponse(HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.");
        }
    }

    private static APIGatewayProxyResponse ErrorResponse(HttpStatusCode statusCode, string code, string message)
    {
        var payload = JsonSerializer.Serialize(new { error = new { code, message } });
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Body = payload,
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }
        };
    }
}