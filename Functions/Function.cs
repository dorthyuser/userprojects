using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Travelcardlambdacsharp1133Lambda.Models;
using Travelcardlambdacsharp1133Lambda.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Travelcardlambdacsharp1133Lambda;

public class Function
{
    private readonly Service _service;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter(null, allowIntegerValues: false) },
        PropertyNameCaseInsensitive = true
    };

    public Function()
    {
        _service = new Service();
    }

    public async Task<APIGatewayProxyResponse> travelcardlambdacsharp1133(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var clientId = GetHeader(request.Headers, "client_id");
        context.Logger.LogLine($"Controller entry: {request.HttpMethod} {request.Path} client_id={clientId}");

        try
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                LogValidationFailure(context, "client_id", "missing required header");
                return BadRequest("Missing required header 'client_id'.");
            }

            context.Logger.LogLine("Validating request...");
            var body = string.IsNullOrWhiteSpace(request.Body) ? null : JsonSerializer.Deserialize<CreateTravelcardRequest>(request.Body, JsonOptions);
            if (body is null)
            {
                LogValidationFailure(context, "body", "invalid or empty request body");
                return BadRequest("Invalid request body.");
            }

            var validationError = ValidateRequest(body);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                return BadRequest(validationError);
            }

            context.Logger.LogLine("Validation passed.");
            var result = await _service.CreateAsync(body, context);
            context.Logger.LogLine($"Response: generated ID {result.TravelcardId}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
                Body = JsonSerializer.Serialize(result, JsonOptions)
            };
        }
        catch (JsonException ex)
        {
            context.Logger.LogLine($"Exception: {ex.Message} context=deserialization");
            return BadRequest("Invalid JSON payload.");
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Exception: {ex.Message} context=handler");
            return StatusCode(500, new ErrorResponse("Internal server error."));
        }
    }

    private static string? GetHeader(IDictionary<string, string>? headers, string name)
    {
        if (headers is null) return null;
        foreach (var kv in headers)
        {
            if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase)) return kv.Value;
        }
        return null;
    }

    private static string? ValidateRequest(CreateTravelcardRequest request)
    {
        var acceptedTravelcardTypes = EnumHelper.GetNames<TravelcardType>();
        var acceptedCardholderTypes = EnumHelper.GetNames<CardholderType>();

        if (!Enum.IsDefined(typeof(TravelcardType), request.TravelcardType))
        {
            return $"Invalid value for field 'travelcardType'. Accepted values: {string.Join(", ", acceptedTravelcardTypes)}";
        }

        if (request.TravelcardRequestedDate >= DateTimeOffset.UtcNow)
        {
            return ValidationMessage("travelcardRequestedDate", "requested_date must be in the past");
        }

        if (request.TravelcardValidFrom > request.TravelcardValidTo)
        {
            return ValidationMessage("travelcardValidFrom", "valid_from date is later than valid_to date");
        }

        if (request.TravelcardValidTo <= DateTimeOffset.UtcNow)
        {
            return ValidationMessage("travelcardValidTo", "valid_to date must be in the future");
        }

        if (request.TravelcardValidFrom > DateTimeOffset.UtcNow.AddMonths(1))
        {
            return ValidationMessage("travelcardValidFrom", "valid_from date must be no later than one calendar month from today");
        }

        if (request.TravelcardType == TravelcardType.SixteenToSeventeen)
        {
            if (!request.TravelcardUsableTo.HasValue)
            {
                return ValidationMessage("travelcardUsableTo", "usable_to date is required for SixteenToSeventeen");
            }
        }
        else if (request.TravelcardUsableTo.HasValue && request.TravelcardUsableTo.Value <= DateTimeOffset.UtcNow)
        {
            return ValidationMessage("travelcardUsableTo", "usable_to date must be in the future");
        }

        if (request.TravelcardType is TravelcardType.SixteenToSeventeen or TravelcardType.Veterans)
        {
            if (request.Cardholders.Any(c => c.CardholderType == CardholderType.Secondary))
            {
                return ValidationMessage("cardholders", "secondary cardholder is not allowed for this travelcard type");
            }
        }

        if (request.Cardholders is null || request.Cardholders.Count is < 1 or > 2)
        {
            return ValidationMessage("cardholders", "must contain exactly one primary and optionally one secondary cardholder");
        }

        if (!request.Cardholders.Any(c => c.CardholderType == CardholderType.Primary))
        {
            return ValidationMessage("cardholders", "primary cardholder is required");
        }

        foreach (var cardholder in request.Cardholders)
        {
            if (!Enum.IsDefined(typeof(CardholderType), cardholder.CardholderType))
            {
                return $"Invalid value for field 'cardholderType'. Accepted values: {string.Join(", ", acceptedCardholderTypes)}";
            }
            if (string.IsNullOrWhiteSpace(cardholder.CardholderTitle) || cardholder.CardholderTitle.Length > 15) return ValidationMessage("cardholderTitle", "length must be between 1 and 15");
            if (string.IsNullOrWhiteSpace(cardholder.CardholderForename) || cardholder.CardholderForename.Length > 100) return ValidationMessage("cardholderForename", "length must be between 1 and 100");
            if (string.IsNullOrWhiteSpace(cardholder.CardholderSurname) || cardholder.CardholderSurname.Length > 100) return ValidationMessage("cardholderSurname", "length must be between 1 and 100");
            if (string.IsNullOrWhiteSpace(cardholder.CardholderPhotoName) || cardholder.CardholderPhotoName.Length > 100) return ValidationMessage("cardholderPhotoName", "length must be between 1 and 100");

            var imageCount = new[] { cardholder.CardholderPhotoRRSKey, cardholder.CardholderPhotoURL, cardholder.CardholderPhotoKey }.Count(x => !string.IsNullOrWhiteSpace(x));
            if (imageCount != 1) return ValidationMessage("cardholderPhoto", "exactly one of cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key is required");
        }

        return null;
    }

    private static string ValidationMessage(string field, string reason)
    {
        return $"Validation failed: {field} - {reason}";
    }

    private static APIGatewayProxyResponse BadRequest(string message)
    {
        return StatusCode(400, new ErrorResponse(message));
    }

    private static APIGatewayProxyResponse StatusCode(int code, ErrorResponse error)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = code,
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            Body = JsonSerializer.Serialize(error)
        };
    }

    private static void LogValidationFailure(ILambdaContext context, string field, string reason)
    {
        context.Logger.LogLine($"Validation failed: {field} - {reason}");
    }
}
