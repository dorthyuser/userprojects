using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Npgsql;
using Npgsql.NameTranslation;
using Travelcardlambdacsharp847Lambda.Models;
using Travelcardlambdacsharp847Lambda.Services;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Travelcardlambdacsharp847Lambda;

public class Function
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(null, allowIntegerValues: false) }
    };

    private readonly Service _service = new();

    public async Task<APIGatewayProxyResponse> travelcardlambdacsharp847(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var clientId = GetHeader(request.Headers, "client_id");
        context.Logger.LogLine($"Controller entry: method={request.HttpMethod}, route={request.Path}, client_id={clientId ?? string.Empty}");

        try
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                context.Logger.LogLine("Validation failure: client_id required");
                return BadRequest("Missing required header 'client_id'.");
            }

            context.Logger.LogLine("Validating request...");
            var model = JsonSerializer.Deserialize<Request>(request.Body ?? string.Empty, JsonOptions);
            if (model is null)
            {
                context.Logger.LogLine("Validation failure: body malformed");
                return BadRequest("Invalid request body.");
            }

            var validationError = ValidateRequest(model, context);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                return BadRequest(validationError);
            }

            context.Logger.LogLine("Validation passed.");
            var result = await _service.CreateTravelcardAsync(model, context);
            context.Logger.LogLine($"Response: generated ID={result.TravelcardId}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
                Body = JsonSerializer.Serialize(result, JsonOptions)
            };
        }
        catch (JsonException ex)
        {
            context.Logger.LogLine($"Exception: {ex.Message}; context=deserialization");
            return BadRequest("Invalid JSON payload.");
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Exception: {ex.Message}; context=handler");
            return StatusError("An unexpected error occurred.");
        }
    }

    private static string? ValidateRequest(Request model, ILambdaContext context)
    {
        var allowedTravelcardTypes = EnumHelper.GetValues<TravelcardTypeEnum>();
        if (!Enum.IsDefined(typeof(TravelcardTypeEnum), model.TravelcardType))
        {
            context.Logger.LogLine($"Validation failure: travelcardType invalid enum value. Accepted: [{string.Join(", ", allowedTravelcardTypes)}]");
            return $"Invalid value for field 'travelcardType'. Accepted values: {string.Join(", ", allowedTravelcardTypes)}";
        }

        if (model.TravelcardRequestedDate >= DateTimeOffset.UtcNow)
        {
            context.Logger.LogLine("Validation failure: travelcardRequestedDate must be in the past");
            return "travelcardRequestedDate must be in the past.";
        }

        if (model.TravelcardValidFrom > model.TravelcardValidTo)
        {
            context.Logger.LogLine("Validation failure: travelcardValidFrom later than travelcardValidTo");
            return "travelcardValidFrom must not be later than travelcardValidTo.";
        }

        if (model.TravelcardValidTo <= DateTimeOffset.UtcNow)
        {
            context.Logger.LogLine("Validation failure: travelcardValidTo must be in the future");
            return "travelcardValidTo must be in the future.";
        }

        if (model.TravelcardValidFrom > DateTimeOffset.UtcNow.AddMonths(1))
        {
            context.Logger.LogLine("Validation failure: travelcardValidFrom beyond one month");
            return "travelcardValidFrom must be no later than one calendar month from today.";
        }

        if (model.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen)
        {
            if (!model.TravelcardUsableTo.HasValue)
            {
                context.Logger.LogLine("Validation failure: travelcardUsableTo required for SixteenToSeventeen");
                return "travelcardUsableTo is required for SixteenToSeventeen travelcards.";
            }
        }
        else if (model.TravelcardUsableTo.HasValue && model.TravelcardUsableTo <= DateTimeOffset.UtcNow)
        {
            context.Logger.LogLine("Validation failure: travelcardUsableTo must be in the future");
            return "travelcardUsableTo must be in the future.";
        }

        if (model.TravelcardType is TravelcardTypeEnum.SixteenToSeventeen or TravelcardTypeEnum.Veterans)
        {
            if (model.Cardholders.Any(x => x.CardholderType == CardholderTypeEnum.Secondary))
            {
                context.Logger.LogLine("Validation failure: secondary cardholder not allowed for travelcard type");
                return "Secondary cardholder is not allowed for the selected travelcard type.";
            }
        }

        if (model.Cardholders.Count is < 1 or > 2)
        {
            context.Logger.LogLine("Validation failure: cardholders count invalid");
            return "cardholders must contain exactly one primary and optionally one secondary cardholder.";
        }

        if (model.Cardholders.Count(x => x.CardholderType == CardholderTypeEnum.Primary) != 1)
        {
            context.Logger.LogLine("Validation failure: primary cardholder missing");
            return "Exactly one primary cardholder is required.";
        }

        if (model.Cardholders.Count(x => x.CardholderType == CardholderTypeEnum.Secondary) > 1)
        {
            context.Logger.LogLine("Validation failure: too many secondary cardholders");
            return "Only one secondary cardholder is allowed.";
        }

        foreach (var cardholder in model.Cardholders)
        {
            if (cardholder.CardholderPhotoRRSKey is null && cardholder.CardholderPhotoURL is null && cardholder.CardholderPhotoKey is null)
            {
                context.Logger.LogLine("Validation failure: missing cardholder image detail");
                return "Each cardholder requires one photo detail: cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey.";
            }
        }

        return null;
    }

    private static string? GetHeader(IDictionary<string, string>? headers, string key)
    {
        if (headers is null)
        {
            return null;
        }

        return headers.FirstOrDefault(h => string.Equals(h.Key, key, StringComparison.OrdinalIgnoreCase)).Value;
    }

    private static APIGatewayProxyResponse BadRequest(string message) => new()
    {
        StatusCode = (int)HttpStatusCode.BadRequest,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = JsonSerializer.Serialize(new { message })
    };

    private static APIGatewayProxyResponse StatusError(string message) => new()
    {
        StatusCode = (int)HttpStatusCode.InternalServerError,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = JsonSerializer.Serialize(new { message })
    };
}
