using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace DemotravelcardsLambda;

public class Function
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(null, allowIntegerValues: false) }
    };

    public async Task<APIGatewayProxyResponse> demotravelcards(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            var validationError = ValidateRequest(request, out var model);
            if (!string.IsNullOrEmpty(validationError))
            {
                return Error(HttpStatusCode.BadRequest, validationError);
            }

            var response = new CreateTravelcardResponse
            {
                TravelcardId = "6",
                Token = "P5SSY6"
            };

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
                Body = JsonSerializer.Serialize(response, JsonOptions)
            };
        }
        catch
        {
            return Error(HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static string? ValidateRequest(APIGatewayProxyRequest request, out CreateTravelcardRequest? model)
    {
        model = null;
        if (request.Headers == null || string.IsNullOrWhiteSpace(GetHeader(request.Headers, "client_id"))) return "Missing required header: client_id";
        var contentType = GetHeader(request.Headers, "Content-Type");
        if (string.IsNullOrWhiteSpace(contentType) || !contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase)) return "Missing or invalid header: Content-Type";
        if (string.IsNullOrWhiteSpace(request.Body)) return "Missing request body";

        try
        {
            model = JsonSerializer.Deserialize<CreateTravelcardRequest>(request.Body, JsonOptions);
        }
        catch
        {
            return "Invalid JSON payload";
        }

        if (model == null) return "Invalid request body";
        if (!Enum.TryParse<TravelcardType>(model.TravelcardType, true, out var travelcardType) || !Enum.IsDefined(typeof(TravelcardType), travelcardType))
            return InvalidEnum("travelcardType", Enum.GetNames<TravelcardType>());

        if (model.Cardholders.Count is < 1 or > 2) return "cardholders must contain exactly one Primary and optionally one Secondary";
        if (!model.Cardholders.Any(c => c.CardholderType == nameof(CardholderType.Primary))) return "cardholders must include exactly one Primary cardholder";
        if (model.Cardholders.Count(c => c.CardholderType == nameof(CardholderType.Primary)) != 1) return "cardholders must include exactly one Primary cardholder";
        if (model.Cardholders.Count(c => c.CardholderType == nameof(CardholderType.Secondary)) > 1) return "Only one Secondary cardholder is allowed";
        if (model.Cardholders.Any(c => c.CardholderType == nameof(CardholderType.Secondary)) && (travelcardType == TravelcardType.SixteenToSeventeen || travelcardType == TravelcardType.Veterans)) return "Secondary cardholder is not allowed for this travelcard type";

        if (model.TravelcardRequestedDate >= DateTimeOffset.UtcNow) return "travelcardRequestedDate must be in the past";
        if (model.TravelcardValidFrom > DateTimeOffset.UtcNow.AddMonths(1)) return "travelcardValidFrom must be no later than one calendar month from today";
        if (model.TravelcardValidFrom > model.TravelcardValidTo) return "travelcardValidFrom cannot be later than travelcardValidTo";
        if (model.TravelcardValidTo <= DateTimeOffset.UtcNow) return "travelcardValidTo must be in the future";
        if (travelcardType == TravelcardType.SixteenToSeventeen && model.TravelcardUsableTo == null) return "travelcardUsableTo is required for SixteenToSeventeen travelcard type";
        if (model.TravelcardUsableTo != null && model.TravelcardUsableTo <= DateTimeOffset.UtcNow) return "travelcardUsableTo must be in the future";

        return null;
    }

    private static string InvalidEnum(string fieldName, string[] values) => $"Invalid value for field '{fieldName}'. Accepted values: {string.Join(", ", values)}";

    private static string? GetHeader(IDictionary<string, string>? headers, string name)
    {
        if (headers == null) return null;
        return headers.FirstOrDefault(kvp => string.Equals(kvp.Key, name, StringComparison.OrdinalIgnoreCase)).Value;
    }

    private static APIGatewayProxyResponse Error(HttpStatusCode code, string message) => new()
    {
        StatusCode = (int)code,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = JsonSerializer.Serialize(new { message }, JsonOptions)
    };
}