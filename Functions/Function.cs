using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Travelcardlambdacsharp1105Lambda.Models;
using Travelcardlambdacsharp1105Lambda.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Travelcardlambdacsharp1105Lambda;

public class Function
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(null, allowIntegerValues: false) }
    };

    public async Task<APIGatewayProxyResponse> travelcardlambdacsharp1105(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var logger = context.Logger;
        string clientId = GetHeaderValue(request.Headers, "client_id") ?? string.Empty;
        logger.LogLine($"HTTP {request.HttpMethod} {request.Path} client_id={clientId}");
        try
        {
            if (string.IsNullOrWhiteSpace(clientId) || clientId.Length < 1 || clientId.Length > 128)
                return Error(400, "Validation failed for field 'client_id'. Reason: required and must be 1-128 characters.");

            if (!string.Equals(GetHeaderValue(request.Headers, "Content-Type"), "application/json", StringComparison.OrdinalIgnoreCase))
                return Error(400, "Validation failed for field 'Content-Type'. Reason: must be application/json.");

            var body = string.IsNullOrWhiteSpace(request.Body) ? null : JsonSerializer.Deserialize<CreateTravelcardRequest>(request.Body!, JsonOptions);
            if (body is null)
                return Error(400, "Validation failed for field 'body'. Reason: request body is required.");

            var validationError = ValidateRequest(body);
            if (validationError is not null)
                return Error(400, validationError);

            var service = new Service(logger);
            var result = await service.CreateAsync(body, request.Headers, request.QueryStringParameters, request.PathParameters);
            return new APIGatewayProxyResponse
            {
                StatusCode = 201,
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
                Body = JsonSerializer.Serialize(result, JsonOptions)
            };
        }
        catch
        {
            return Error(500, "An unexpected error occurred.");
        }
    }

    private static string? GetHeaderValue(IDictionary<string, string>? headers, string key)
    {
        if (headers is null) return null;
        return headers.FirstOrDefault(kvp => string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase)).Value;
    }

    private static string? ValidateRequest(CreateTravelcardRequest request)
    {
        if (!Enum.IsDefined(typeof(TravelcardTypeEnum), request.TravelcardType))
            return $"Invalid value for field 'travelcardType'. Accepted values: {string.Join(", ", Enum.GetNames(typeof(TravelcardTypeEnum)))}";

        if (request.TravelcardRequestedDate >= DateTimeOffset.UtcNow)
            return "Validation failed for field 'travelcardRequestedDate'. Reason: requested date must be in the past.";

        if (request.TravelcardValidFrom > request.TravelcardValidTo)
            return "Validation failed for field 'travelcardValidFrom'. Reason: valid_from date cannot be later than valid_to date.";

        if (request.TravelcardValidTo <= DateTimeOffset.UtcNow)
            return "Validation failed for field 'travelcardValidTo'. Reason: valid_to date must be in the future.";

        if (request.TravelcardValidFrom > DateTimeOffset.UtcNow.AddMonths(1))
            return "Validation failed for field 'travelcardValidFrom'. Reason: valid_from must be no later than one calendar month from today.";

        if (request.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen && request.TravelcardUsableTo is null)
            return "Validation failed for field 'travelcardUsableTo'. Reason: usable_to is required for SixteenToSeventeen travelcards.";

        if (request.TravelcardUsableTo is not null && request.TravelcardUsableTo <= DateTimeOffset.UtcNow)
            return "Validation failed for field 'travelcardUsableTo'. Reason: usable_to must be in the future.";

        if (request.Cardholders is null || request.Cardholders.Count is < 1 or > 2)
            return "Validation failed for field 'cardholders'. Reason: exactly one primary and optional one secondary cardholder are required.";

        if (request.Cardholders.Count(x => x.CardholderType == CardholderTypeEnum.Primary) != 1)
            return "Validation failed for field 'cardholders'. Reason: exactly one primary cardholder is required.";

        if (request.Cardholders.Count(x => x.CardholderType == CardholderTypeEnum.Secondary) > 1)
            return "Validation failed for field 'cardholders'. Reason: only one secondary cardholder is allowed.";

        if ((request.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen || request.TravelcardType == TravelcardTypeEnum.Veterans) && request.Cardholders.Any(x => x.CardholderType == CardholderTypeEnum.Secondary))
            return "Validation failed for field 'cardholders'. Reason: secondary cardholder is not allowed for this travelcard type.";

        foreach (var ch in request.Cardholders)
        {
            if (string.IsNullOrWhiteSpace(ch.CardholderTitle) || ch.CardholderTitle.Length > 15)
                return "Validation failed for field 'cardholderTitle'. Reason: length must be 1-15 characters.";
            if (string.IsNullOrWhiteSpace(ch.CardholderForename) || ch.CardholderForename.Length > 100)
                return "Validation failed for field 'cardholderForename'. Reason: length must be 1-100 characters.";
            if (string.IsNullOrWhiteSpace(ch.CardholderSurname) || ch.CardholderSurname.Length > 100)
                return "Validation failed for field 'cardholderSurname'. Reason: length must be 1-100 characters.";
            if (string.IsNullOrWhiteSpace(ch.CardholderPhotoName) || ch.CardholderPhotoName.Length > 100)
                return "Validation failed for field 'cardholderPhotoName'. Reason: length must be 1-100 characters.";
            var images = new[] { ch.CardholderPhotoRRSKey, ch.CardholderPhotoURL, ch.CardholderPhotoKey }.Count(v => !string.IsNullOrWhiteSpace(v));
            if (images != 1)
                return "Validation failed for field 'cardholders'. Reason: each cardholder must provide exactly one photo reference.";
        }

        return null;
    }

    private static APIGatewayProxyResponse Error(int statusCode, string message) => new()
    {
        StatusCode = statusCode,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = JsonSerializer.Serialize(new { error = message }, JsonOptions)
    };
}