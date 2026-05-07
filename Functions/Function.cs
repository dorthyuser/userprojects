using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Npgsql;
using Npgsql.NameTranslation;
using Travelcardcharplambda225Lambda.Models;
using Travelcardcharplambda225Lambda.Services;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Travelcardcharplambda225Lambda;

public class Function
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(null, allowIntegerValues: false) }
    };

    private readonly Service _service;

    public Function()
    {
        _service = new Service();
    }

    public async Task<APIGatewayProxyResponse> travelcardcharplambda225(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var logger = context.Logger;
        var method = request.HttpMethod ?? string.Empty;
        var route = request.Path ?? string.Empty;
        var clientId = GetHeader(request.Headers, "client_id");
        logger.LogLine($"Controller entry: method={method}, route={route}, client_id={clientId}");

        try
        {
            logger.LogLine("Validating request...");
            var validation = RequestValidator.Validate(request);
            if (!validation.IsValid)
            {
                logger.LogLine($"Validation failure: {validation.FieldName} - {validation.Reason}");
                return BuildResponse(HttpStatusCode.BadRequest, new ErrorResponse(validation.FieldName, validation.Message));
            }
            logger.LogLine("Validation passed.");

            var body = JsonSerializer.Deserialize<Request>(request.Body ?? string.Empty, JsonOptions);
            if (body is null)
            {
                logger.LogLine("Validation failure: body - invalid or empty JSON");
                return BuildResponse(HttpStatusCode.BadRequest, new ErrorResponse("body", "Invalid JSON body."));
            }

            var bodyValidation = RequestValidator.ValidateBody(body);
            if (!bodyValidation.IsValid)
            {
                logger.LogLine($"Validation failure: {bodyValidation.FieldName} - {bodyValidation.Reason}");
                return BuildResponse(HttpStatusCode.BadRequest, new ErrorResponse(bodyValidation.FieldName, bodyValidation.Message));
            }

            var result = await _service.CreateTravelcardAsync(body, logger);
            logger.LogLine($"Response generated ID: {result.TravelcardId}");
            return BuildResponse(HttpStatusCode.OK, result);
        }
        catch (JsonException ex)
        {
            logger.LogLine($"Exception: {ex.Message} | context=JSON deserialization");
            return BuildResponse(HttpStatusCode.BadRequest, new ErrorResponse("body", "Invalid JSON body."));
        }
        catch (Exception ex)
        {
            logger.LogLine($"Exception: {ex.Message} | context=travelcard creation");
            return BuildResponse(HttpStatusCode.InternalServerError, new ErrorResponse("internal_error", "An unexpected error occurred."));
        }
    }

    private static string GetHeader(IDictionary<string, string>? headers, string name)
    {
        if (headers is null) return string.Empty;
        foreach (var kvp in headers)
        {
            if (string.Equals(kvp.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value ?? string.Empty;
            }
        }
        return string.Empty;
    }

    private static APIGatewayProxyResponse BuildResponse(HttpStatusCode statusCode, object body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            },
            Body = JsonSerializer.Serialize(body, JsonOptions)
        };
    }
}

public static class RequestValidator
{
    private static readonly string[] TravelcardTypes = Enum.GetNames(typeof(TravelcardType));
    private static readonly string[] CardholderTypes = Enum.GetNames(typeof(CardholderType));

    public static ValidationResult Validate(APIGatewayProxyRequest request)
    {
        var clientId = GetHeader(request.Headers, "client_id");
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return ValidationResult.Fail("client_id", "client_id is required.");
        }
        if (clientId.Length is < 1 or > 128)
        {
            return ValidationResult.Fail("client_id", "client_id length must be between 1 and 128 characters.");
        }
        var contentType = GetHeader(request.Headers, "Content-Type");
        if (!string.IsNullOrWhiteSpace(contentType) && !string.Equals(contentType, "application/json", StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Fail("Content-Type", "Content-Type must be application/json.");
        }
        return ValidationResult.Ok();
    }

    public static ValidationResult ValidateBody(Request request)
    {
        if (!Enum.IsDefined(typeof(TravelcardType), request.TravelcardType))
        {
            return ValidationResult.Fail("travelcardType", $"invalid enum value. Accepted: [{string.Join(", ", TravelcardTypes)}]");
        }
        if (!Enum.IsDefined(typeof(CardholderType), request.Cardholders[0].CardholderType))
        {
            return ValidationResult.Fail("cardholderType", $"invalid enum value. Accepted: [{string.Join(", ", CardholderTypes)}]");
        }
        if (request.TravelcardRequestedDate >= DateTimeOffset.UtcNow)
        {
            return ValidationResult.Fail("travelcardRequestedDate", "requested_date must be in the past.");
        }
        if (request.TravelcardValidFrom > request.TravelcardValidTo)
        {
            return ValidationResult.Fail("travelcardValidFrom", "valid_from date is later than valid_to date.");
        }
        if (request.TravelcardValidTo <= DateTimeOffset.UtcNow)
        {
            return ValidationResult.Fail("travelcardValidTo", "valid_to date must be in the future.");
        }
        if (request.TravelcardValidFrom > DateTimeOffset.UtcNow.AddMonths(1))
        {
            return ValidationResult.Fail("travelcardValidFrom", "valid_from must be no later than one calendar month from today.");
        }
        if (request.TravelcardType == TravelcardType.SixteenToSeventeen)
        {
            if (request.TravelcardUsableTo is null)
                return ValidationResult.Fail("travelcardUsableTo", "usable_to date is required for SixteenToSeventeen travelcard type.");
            if (request.TravelcardUsableTo <= DateTimeOffset.UtcNow)
                return ValidationResult.Fail("travelcardUsableTo", "usable_to date must be in the future.");
        }
        if (request.TravelcardType is TravelcardType.SixteenToSeventeen or TravelcardType.Veterans)
        {
            if (request.Cardholders.Exists(c => c.CardholderType == CardholderType.Secondary))
            {
                return ValidationResult.Fail("cardholders", "secondary cardholder is not allowed for this travelcard type.");
            }
        }
        if (request.Cardholders.Count is < 1 or > 2)
        {
            return ValidationResult.Fail("cardholders", "cardholders must contain exactly one or two items.");
        }
        if (!request.Cardholders.Exists(c => c.CardholderType == CardholderType.Primary))
        {
            return ValidationResult.Fail("cardholders", "exactly one primary cardholder is required.");
        }
        return ValidationResult.Ok();
    }

    private static string GetHeader(IDictionary<string, string>? headers, string name)
    {
        if (headers is null) return string.Empty;
        foreach (var kvp in headers)
        {
            if (string.Equals(kvp.Key, name, StringComparison.OrdinalIgnoreCase)) return kvp.Value ?? string.Empty;
        }
        return string.Empty;
    }
}

public sealed class ValidationResult
{
    public bool IsValid { get; }
    public string FieldName { get; }
    public string Reason { get; }
    public string Message { get; }

    private ValidationResult(bool isValid, string fieldName, string reason, string message)
    {
        IsValid = isValid;
        FieldName = fieldName;
        Reason = reason;
        Message = message;
    }

    public static ValidationResult Ok() => new(true, string.Empty, string.Empty, string.Empty);
    public static ValidationResult Fail(string fieldName, string message) => new(false, fieldName, message, message);
}
