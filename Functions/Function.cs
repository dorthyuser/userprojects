using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Microsoft.Extensions.Logging;
using TestCsharpLambdaTc123Lambda.Models;
using TestCsharpLambdaTc123Lambda.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace TestCsharpLambdaTc123Lambda;

public class Function
{
    private readonly ILogger<Function> _logger;
    private readonly Service _service;

    public Function()
    {
        var factory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = factory.CreateLogger<Function>();
        _service = new Service(factory.CreateLogger<Service>());
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        _logger.LogInformation("Received request");
        try
        {
            if (!string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                return ResponseFactory.Error(HttpStatusCode.MethodNotAllowed, "METHOD_NOT_ALLOWED", "Only POST is allowed.");
            }

            if (!request.Headers.TryGetValue("client_id", out var clientId) || string.IsNullOrWhiteSpace(clientId))
            {
                return ResponseFactory.Error(HttpStatusCode.BadRequest, "MISSING_HEADER", "client_id header is required.");
            }

            if (clientId.Length < 1 || clientId.Length > 128 || !System.Text.RegularExpressions.Regex.IsMatch(clientId, @"^[\w+]+$"))
            {
                return ResponseFactory.Error(HttpStatusCode.BadRequest, "INVALID_HEADER", "client_id header is invalid.");
            }

            if (!request.Headers.TryGetValue("Content-Type", out var contentType) || !contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                return ResponseFactory.Error(HttpStatusCode.BadRequest, "INVALID_HEADER", "Content-Type must contain application/json.");
            }

            if (request.Body is null || string.IsNullOrWhiteSpace(request.Body))
            {
                return ResponseFactory.Error(HttpStatusCode.BadRequest, "MISSING_BODY", "Request body is required.");
            }

            var model = JsonSerializer.Deserialize<Request>(request.Body, JsonOptionsFactory.Create());
            if (model is null)
            {
                return ResponseFactory.Error(HttpStatusCode.BadRequest, "INVALID_BODY", "Unable to parse request body.");
            }

            var validationError = Validators.Validate(model);
            if (validationError is not null)
            {
                return ResponseFactory.Error(HttpStatusCode.BadRequest, "VALIDATION_ERROR", validationError);
            }

            var result = await _service.CreateAsync(model);
            return ResponseFactory.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error");
            return ResponseFactory.Error(HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.");
        }
    }
}

public static class JsonOptionsFactory
{
    public static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new ExactEnumJsonConverterFactory());
        return options;
    }
}

public static class Validators
{
    public static string? Validate(Request request)
    {
        if (!Enum.IsDefined(typeof(TravelcardType), request.TravelcardType)) return "Invalid travelcardType.";
        if (request.TravelcardRequestedDate >= DateTimeOffset.UtcNow) return "travelcardRequestedDate must be in the past.";
        if (request.TravelcardValidFrom > request.TravelcardValidTo) return "travelcardValidFrom must be earlier than travelcardValidTo.";
        if (request.TravelcardValidTo <= DateTimeOffset.UtcNow) return "travelcardValidTo must be in the future.";
        if (request.TravelcardType == TravelcardType.SixteenToSeventeen && request.TravelcardUsableTo is null) return "travelcardUsableTo is required for SixteenToSeventeen.";
        if (request.TravelcardUsableTo.HasValue && request.TravelcardUsableTo.Value <= DateTimeOffset.UtcNow) return "travelcardUsableTo must be in the future.";
        if (request.Cardholders is null || request.Cardholders.Count is < 1 or > 2) return "cardholders must contain exactly 1 or 2 items.";
        if (request.Cardholders.Count(c => c.CardholderType == CardholderType.Primary) != 1) return "Exactly one Primary cardholder is required.";
        if (request.Cardholders.Count(c => c.CardholderType == CardholderType.Secondary) > 1) return "Only one Secondary cardholder is allowed.";
        if (request.Cardholders.Any(c => !HasOnePhotoField(c))) return "Each cardholder must have exactly one photo field populated.";
        if (request.Cardholders.Any(c => c.CardholderTitle.Length < 1 || c.CardholderTitle.Length > 15)) return "Invalid cardholderTitle.";
        if (request.Cardholders.Any(c => c.CardholderForename.Length < 1 || c.CardholderForename.Length > 100 || !System.Text.RegularExpressions.Regex.IsMatch(c.CardholderForename, @"^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-ž .''’\-]+$"))) return "Invalid cardholderForename.";
        if (request.Cardholders.Any(c => c.CardholderSurname.Length < 1 || c.CardholderSurname.Length > 100 || !System.Text.RegularExpressions.Regex.IsMatch(c.CardholderSurname, @"^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-ž .''’\-]+$"))) return "Invalid cardholderSurname.";
        if (request.Cardholders.Any(c => c.CardholderPhotoName.Length < 1 || c.CardholderPhotoName.Length > 100 || !System.Text.RegularExpressions.Regex.IsMatch(c.CardholderPhotoName, @"^(?!.*[×÷ˇ˘μ])[A-Za-z0-9À-ž _\.\-()\[\]'',&+#]+$"))) return "Invalid cardholderPhotoName.";
        if (!System.Text.RegularExpressions.Regex.IsMatch(request.TravelcardName ?? string.Empty, @"^[A-Za-z0-9 ]*$")) return "Invalid travelcardName.";
        if (request.TravelcardNumber.Length < 11 || request.TravelcardNumber.Length > 22 || !System.Text.RegularExpressions.Regex.IsMatch(request.TravelcardNumber, @"^[A-Za-z0-9]+$")) return "Invalid travelcardNumber.";
        if (request.TravelcardTransactionReference.Length != 15 || !System.Text.RegularExpressions.Regex.IsMatch(request.TravelcardTransactionReference, @"^[0-9]{2}[A-Z0-9]{4}[0-9]{4}[0-9]{5}$")) return "Invalid travelcardTransactionReference.";
        return null;
    }

    private static bool HasOnePhotoField(Cardholder c)
    {
        var count = 0;
        if (!string.IsNullOrWhiteSpace(c.CardholderPhotoRRSKey)) count++;
        if (!string.IsNullOrWhiteSpace(c.CardholderPhotoURL)) count++;
        if (!string.IsNullOrWhiteSpace(c.CardholderPhotoKey)) count++;
        return count == 1;
    }
}

public static class ResponseFactory
{
    public static APIGatewayProxyResponse Success(Response result) => new()
    {
        StatusCode = 201,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = JsonSerializer.Serialize(result, JsonOptionsFactory.Create())
    };

    public static APIGatewayProxyResponse Error(HttpStatusCode statusCode, string code, string message) => new()
    {
        StatusCode = (int)statusCode,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = JsonSerializer.Serialize(new { error = new { code, message } }, JsonOptionsFactory.Create())
    };
}