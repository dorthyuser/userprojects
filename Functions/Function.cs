using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Travelcardlambda1010Lambda.Models;
using Travelcardlambda1010Lambda.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Travelcardlambda1010Lambda;

public class Function
{
    private readonly Service _service;

    public Function()
    {
        _service = new Service();
    }

    public async Task<APIGatewayProxyResponse> travelcardlambda1010(APIGatewayProxyRequest request, ILambdaContext context)
    {
        Console.WriteLine("Starting travelcardlambda1010 handler");

        try
        {
            var clientId = GetHeader(request, "client_id");
            var correlationId = GetHeader(request, "X-Correlation-Cust-Id");
            var contentType = GetHeader(request, "Content-Type");

            if (string.IsNullOrWhiteSpace(clientId) || clientId.Length < 1 || clientId.Length > 128)
            {
                return Error(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "client_id is required and must be between 1 and 128 characters.", correlationId);
            }

            if (!string.IsNullOrWhiteSpace(contentType) && !string.Equals(contentType, "application/json", StringComparison.OrdinalIgnoreCase))
            {
                return Error(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "Content-Type must be application/json.", correlationId);
            }

            if (string.IsNullOrWhiteSpace(request.Body))
            {
                return Error(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "Request body is required.", correlationId);
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var body = JsonSerializer.Deserialize<TravelcardRequest>(request.Body, options);
            if (body is null)
            {
                return Error(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "Invalid JSON body.", correlationId);
            }

            var validationError = ValidationHelper.Validate(body);
            if (validationError is not null)
            {
                return Error(HttpStatusCode.BadRequest, "VALIDATION_ERROR", validationError, correlationId);
            }

            var response = await _service.CreateTravelcardAsync(body);
            Console.WriteLine("Travelcard created successfully");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "X-Correlation-Cust-Id", correlationId ?? string.Empty }
                },
                Body = JsonSerializer.Serialize(response)
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unhandled error: {ex}");
            return Error(HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.", GetHeader(request, "X-Correlation-Cust-Id"));
        }
    }

    private static string? GetHeader(APIGatewayProxyRequest request, string name)
    {
        if (request.Headers is null)
        {
            return null;
        }

        return request.Headers.TryGetValue(name, out var value) ? value : request.Headers.FirstOrDefault(h => string.Equals(h.Key, name, StringComparison.OrdinalIgnoreCase)).Value;
    }

    private static APIGatewayProxyResponse Error(HttpStatusCode status, string code, string message, string? correlationId)
    {
        var payload = new ErrorResponse
        {
            Error = new ErrorDetail
            {
                Code = code,
                Message = message,
                CorrelationId = correlationId
            }
        };

        return new APIGatewayProxyResponse
        {
            StatusCode = (int)status,
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "X-Correlation-Cust-Id", correlationId ?? string.Empty }
            },
            Body = JsonSerializer.Serialize(payload)
        };
    }
}

public static class ValidationHelper
{
    public static string? Validate(TravelcardRequest request)
    {
        if (request.TravelcardValidFrom > DateTimeOffset.UtcNow.AddMonths(1))
        {
            return "travelcardValidFrom must be no later than one calendar month from today.";
        }

        if (request.TravelcardRequestedDate >= DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return "travelcardRequestedDate must be in the past.";
        }

        if (request.TravelcardValidFrom > request.TravelcardValidTo)
        {
            return "travelcardValidFrom cannot be later than travelcardValidTo.";
        }

        if (request.TravelcardValidTo <= DateTimeOffset.UtcNow)
        {
            return "travelcardValidTo must be in the future.";
        }

        if (request.TravelcardType == TravelcardType.SixteenToSeventeen && request.TravelcardUsableTo is null)
        {
            return "travelcardUsableTo is required for SixteenToSeventeen travelcards.";
        }

        if (request.TravelcardUsableTo is not null && request.TravelcardUsableTo <= DateTimeOffset.UtcNow)
        {
            return "travelcardUsableTo must be in the future when provided.";
        }

        if (request.TravelcardType != TravelcardType.SixteenToSeventeen && request.TravelcardUsableTo is not null)
        {
            return "travelcardUsableTo is only allowed for SixteenToSeventeen travelcards.";
        }

        if (request.Cardholders is null || request.Cardholders.Count is < 1 or > 2)
        {
            return "cardholders must contain exactly one primary cardholder and optionally one secondary cardholder.";
        }

        if (!request.Cardholders.Any(c => c.CardholderType == CardholderType.Primary))
        {
            return "Exactly one primary cardholder is required.";
        }

        if (request.Cardholders.Count(c => c.CardholderType == CardholderType.Primary) != 1)
        {
            return "Only one primary cardholder is allowed.";
        }

        if (request.Cardholders.Count(c => c.CardholderType == CardholderType.Secondary) > 1)
        {
            return "Only one secondary cardholder is allowed.";
        }

        if (request.Cardholders.Any(c => c.CardholderType == CardholderType.Secondary) && request.TravelcardType is TravelcardType.SixteenToSeventeen or TravelcardType.Veterans)
        {
            return "Secondary cardholder is not allowed for SixteenToSeventeen and Veterans travelcard types.";
        }

        foreach (var holder in request.Cardholders)
        {
            if (string.IsNullOrWhiteSpace(holder.CardholderTitle) || holder.CardholderTitle.Length > 15)
            {
                return "cardholderTitle is required and must be up to 15 characters.";
            }

            if (string.IsNullOrWhiteSpace(holder.CardholderForename) || holder.CardholderForename.Length > 100)
            {
                return "cardholderForename is required and must be up to 100 characters.";
            }

            if (string.IsNullOrWhiteSpace(holder.CardholderSurname) || holder.CardholderSurname.Length > 100)
            {
                return "cardholderSurname is required and must be up to 100 characters.";
            }

            if (string.IsNullOrWhiteSpace(holder.CardholderPhotoName) || holder.CardholderPhotoName.Length > 100)
            {
                return "cardholderPhotoName is required and must be up to 100 characters.";
            }

            var photoCount = new[] { holder.CardholderPhotoRRSKey, holder.CardholderPhotoURL, holder.CardholderPhotoKey }.Count(v => !string.IsNullOrWhiteSpace(v));
            if (photoCount != 1)
            {
                return "Each cardholder must provide exactly one of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey.";
            }
        }

        return null;
    }
}
