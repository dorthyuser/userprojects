using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Npgsql;
using Npgsql.NameTranslation;
using Travelcardcharplambda1031Lambda.Models;
using Travelcardcharplambda1031Lambda.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Travelcardcharplambda1031Lambda;

public class Function
{
    private readonly Service _service;

    public Function()
    {
        _service = new Service();
    }

    public async Task<APIGatewayProxyResponse> travelcardcharplambda1031(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var logger = context.Logger;
        var method = request.HttpMethod ?? string.Empty;
        var route = request.Path ?? string.Empty;
        var clientId = HeaderHelper.GetHeaderValue(request.Headers, "client_id") ?? string.Empty;
        logger.LogLine($"Entry: method={method}, route={route}, client_id={clientId}");

        try
        {
            var body = string.IsNullOrWhiteSpace(request.Body) ? "{}" : request.Body;
            var jsonOptions = new JsonSerializerOptions();
            jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(null, allowIntegerValues: false));
            var payload = JsonSerializer.Deserialize<CreateTravelcardRequest>(body, jsonOptions);

            if (payload is null)
            {
                logger.LogLine("Validation failed: request body is invalid JSON.");
                return ApiResponse.BadRequest("Invalid request body.");
            }

            var validation = RequestValidator.Validate(payload);
            if (!validation.IsValid)
            {
                logger.LogLine($"Validation failed: {validation.ErrorMessage}");
                return ApiResponse.BadRequest(validation.ErrorMessage);
            }

            logger.LogLine("Validating request...");
            logger.LogLine("Validation passed.");

            var result = await _service.CreateTravelcardAsync(payload, context);
            logger.LogLine($"Response generated id={result.TravelcardId}");

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json"
                },
                Body = JsonSerializer.Serialize(result)
            };
        }
        catch (NpgsqlException ex)
        {
            logger.LogLine($"Exception: database error in travelcard creation: {ex.Message}");
            return ApiResponse.InternalServerError("A database error occurred.");
        }
        catch (Exception ex)
        {
            logger.LogLine($"Exception: unexpected error in travelcard creation: {ex.Message}");
            return ApiResponse.InternalServerError("An unexpected error occurred.");
        }
    }
}

internal static class HeaderHelper
{
    public static string? GetHeaderValue(IDictionary<string, string>? headers, string headerName)
    {
        if (headers is null || string.IsNullOrWhiteSpace(headerName))
        {
            return null;
        }

        foreach (var item in headers)
        {
            if (string.Equals(item.Key, headerName, StringComparison.OrdinalIgnoreCase))
            {
                return item.Value;
            }
        }

        return null;
    }
}

internal static class ApiResponse
{
    public static APIGatewayProxyResponse BadRequest(string message) => new()
    {
        StatusCode = (int)HttpStatusCode.BadRequest,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = JsonSerializer.Serialize(new ErrorResponse(message))
    };

    public static APIGatewayProxyResponse InternalServerError(string message) => new()
    {
        StatusCode = (int)HttpStatusCode.InternalServerError,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = JsonSerializer.Serialize(new ErrorResponse(message))
    };
}

internal static class RequestValidator
{
    public static ValidationResult Validate(CreateTravelcardRequest request)
    {
        if (!request.TravelcardRequestedDate.IsPast())
        {
            return ValidationResult.Fail("travelcardRequestedDate must be in the past.");
        }

        if (request.TravelcardValidFrom > request.TravelcardValidTo)
        {
            return ValidationResult.Fail("travelcardValidFrom must not be later than travelcardValidTo.");
        }

        if (request.TravelcardValidTo <= DateTimeOffset.UtcNow)
        {
            return ValidationResult.Fail("travelcardValidTo must be in the future.");
        }

        if (request.TravelcardValidFrom > DateTimeOffset.UtcNow.AddMonths(1))
        {
            return ValidationResult.Fail("travelcardValidFrom must be no later than one calendar month from today.");
        }

        if (request.TravelcardType == TravelcardType.SixteenToSeventeen)
        {
            if (!request.TravelcardUsableTo.HasValue)
            {
                return ValidationResult.Fail("travelcardUsableTo is required for SixteenToSeventeen.");
            }

            if (request.TravelcardUsableTo.Value <= DateTimeOffset.UtcNow)
            {
                return ValidationResult.Fail("travelcardUsableTo must be in the future.");
            }
        }
        else if (request.TravelcardUsableTo.HasValue && request.TravelcardUsableTo.Value <= DateTimeOffset.UtcNow)
        {
            return ValidationResult.Fail("travelcardUsableTo must be in the future.");
        }

        if (request.Cardholders.Count is not (1 or 2))
        {
            return ValidationResult.Fail("cardholders must contain exactly one or two items.");
        }

        if (!request.Cardholders.Any(c => c.CardholderType == CardholderType.Primary))
        {
            return ValidationResult.Fail("A primary cardholder is required.");
        }

        if (request.Cardholders.Count(c => c.CardholderType == CardholderType.Secondary) > 1)
        {
            return ValidationResult.Fail("Only one secondary cardholder is allowed.");
        }

        if (request.Cardholders.Any(c => c.CardholderType == CardholderType.Secondary) && (request.TravelcardType == TravelcardType.SixteenToSeventeen || request.TravelcardType == TravelcardType.Veterans))
        {
            return ValidationResult.Fail("Secondary cardholder is not allowed for this travelcard type.");
        }

        foreach (var cardholder in request.Cardholders)
        {
            var photoCount = new[] { cardholder.CardholderPhotoRRSKey, cardholder.CardholderPhotoURL, cardholder.CardholderPhotoKey }.Count(x => !string.IsNullOrWhiteSpace(x));
            if (photoCount != 1)
            {
                return ValidationResult.Fail("Each cardholder must provide exactly one photo detail.");
            }
        }

        return ValidationResult.Success();
    }
}

internal static class DateTimeExtensions
{
    public static bool IsPast(this DateTimeOffset dateTime) => dateTime < DateTimeOffset.UtcNow;
}

internal sealed record ValidationResult(bool IsValid, string? ErrorMessage)
{
    public static ValidationResult Success() => new(true, null);
    public static ValidationResult Fail(string message) => new(false, message);
}