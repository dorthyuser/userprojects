using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using TravelcardAppDemoLambda.Models;
using TravelcardAppDemoLambda.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace TravelcardAppDemoLambda;

public class Function
{
    private readonly Service _service;

    public Function()
    {
        _service = new Service();
    }

    public async Task<APIGatewayProxyResponse> TravelcardAppDemo(APIGatewayProxyRequest request, ILambdaContext context)
    {
        string clientId = GetHeader(request.Headers, "client_id") ?? string.Empty;
        context.Logger.LogLine($"HTTP method: {request.HttpMethod}, route: {request.Path}, client_id: {clientId}");

        try
        {
            var validationError = RequestValidator.Validate(request, out RequestModel? model);
            if (validationError != null)
            {
                context.Logger.LogLine($"Validation failed: {validationError.Field} - {validationError.Reason}");
                return ApiResponse.BadRequest(validationError.Message);
            }

            var created = await _service.CreateTravelcardAsync(model!, context);
            context.Logger.LogLine($"Response generated ID: {created.TravelcardId}");
            return ApiResponse.Ok(created);
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Exception in travelcard-app-demo: {ex.Message}");
            return ApiResponse.InternalServerError("An unexpected error occurred.");
        }
    }

    private static string? GetHeader(IDictionary<string, string>? headers, string name)
    {
        if (headers == null) return null;
        foreach (var kvp in headers)
        {
            if (string.Equals(kvp.Key, name, StringComparison.OrdinalIgnoreCase)) return kvp.Value;
        }
        return null;
    }
}

internal static class RequestValidator
{
    public static ValidationError? Validate(APIGatewayProxyRequest request, out RequestModel? model)
    {
        model = null;
        if (request.Headers == null || !HasHeader(request.Headers, "client_id"))
        {
            return new ValidationError("client_id", "missing required header");
        }

        if (!HasHeader(request.Headers, "Content-Type") || !GetHeader(request.Headers, "Content-Type")!.Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            return new ValidationError("Content-Type", "must contain application/json");
        }

        try
        {
            model = System.Text.Json.JsonSerializer.Deserialize<RequestModel>(request.Body ?? string.Empty, JsonOptions.Default);
        }
        catch
        {
            return new ValidationError("body", "invalid JSON payload");
        }

        if (model == null) return new ValidationError("body", "request body is required");
        return RequestModelValidator.Validate(model);
    }

    private static bool HasHeader(IDictionary<string, string> headers, string name) => headers.Keys.Any(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));
    private static string? GetHeader(IDictionary<string, string> headers, string name) => headers.FirstOrDefault(kvp => string.Equals(kvp.Key, name, StringComparison.OrdinalIgnoreCase)).Value;
}

internal static class ApiResponse
{
    public static APIGatewayProxyResponse Ok(ResponseModel response) => new()
    {
        StatusCode = (int)HttpStatusCode.OK,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = System.Text.Json.JsonSerializer.Serialize(response, JsonOptions.Default)
    };

    public static APIGatewayProxyResponse BadRequest(string message) => new()
    {
        StatusCode = (int)HttpStatusCode.BadRequest,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = System.Text.Json.JsonSerializer.Serialize(new { message }, JsonOptions.Default)
    };

    public static APIGatewayProxyResponse InternalServerError(string message) => new()
    {
        StatusCode = (int)HttpStatusCode.InternalServerError,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = System.Text.Json.JsonSerializer.Serialize(new { message }, JsonOptions.Default)
    };
}

internal sealed record ValidationError(string Field, string Reason)
{
    public string Message => Reason;
}

internal static class JsonOptions
{
    public static readonly System.Text.Json.JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };
}

public sealed class ResponseModel
{
    [System.Text.Json.Serialization.JsonPropertyName("travelcardId")]
    public string TravelcardId { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}