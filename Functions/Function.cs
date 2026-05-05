using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using TravelcardchsarplambdaLambda.Models;
using TravelcardchsarplambdaLambda.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace TravelcardchsarplambdaLambda;

public class Function
{
    private readonly Service _service;

    public Function()
    {
        _service = new Service();
    }

    public async Task<APIGatewayProxyResponse> travelcardchsarplambda(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var logger = context.Logger;
        var headers = new Dictionary<string, string>(request.Headers ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
        try
        {
            logger.LogLine("Handler started");
            if (!headers.TryGetValue("client_id", out var clientId) || string.IsNullOrWhiteSpace(clientId))
            {
                return BuildError(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "client_id is required");
            }

            if (!headers.TryGetValue("Content-Type", out var contentType) || !contentType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
            {
                return BuildError(HttpStatusCode.UnsupportedMediaType, "VALIDATION_ERROR", "Content-Type must be application/json");
            }

            if (string.IsNullOrWhiteSpace(request.Body))
            {
                return BuildError(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "Request body is required");
            }

            var model = JsonSerializer.Deserialize<Request>(request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (model is null)
            {
                return BuildError(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "Invalid request body");
            }

            var validationError = ValidationHelper.Validate(model);
            if (validationError is not null)
            {
                return BuildError(HttpStatusCode.BadRequest, "VALIDATION_ERROR", validationError);
            }

            var result = await _service.CreateAsync(model, logger);
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
                Body = JsonSerializer.Serialize(result)
            };
        }
        catch (Exception ex)
        {
            logger.LogLine($"Unhandled error: {ex}");
            return BuildError(HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred");
        }
    }

    private static APIGatewayProxyResponse BuildError(HttpStatusCode statusCode, string code, string message)
    {
        var body = new ErrorResponse { Error = new ErrorDetail { Code = code, Message = message } };
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            Body = JsonSerializer.Serialize(body)
        };
    }
}

public static class ValidationHelper
{
    public static string? Validate(Request request)
    {
        if (!Enum.IsDefined(typeof(TravelcardTypeEnum), request.TravelcardType)) return "Invalid travelcardType";
        if (request.TravelcardRequestedDate >= DateTimeOffset.UtcNow) return "travelcardRequestedDate must be in the past";
        if (request.TravelcardValidFrom > request.TravelcardValidTo) return "travelcardValidFrom cannot be later than travelcardValidTo";
        if (request.TravelcardValidTo <= DateTimeOffset.UtcNow) return "travelcardValidTo must be in the future";
        if (request.TravelcardValidFrom > DateTimeOffset.UtcNow.AddMonths(1)) return "travelcardValidFrom must not be later than one calendar month from today";
        if (request.TravelcardUsableTo.HasValue && request.TravelcardUsableTo <= DateTimeOffset.UtcNow) return "travelcardUsableTo must be in the future";
        if (request.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen && !request.TravelcardUsableTo.HasValue) return "travelcardUsableTo is required for SixteenToSeventeen";
        if ((request.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen || request.TravelcardType == TravelcardTypeEnum.Veterans) && request.Cardholders.Any(c => c.CardholderType == CardholderTypeEnum.Secondary)) return "Secondary cardholder is not allowed for this travelcard type";
        if (request.Cardholders is null || request.Cardholders.Count is < 1 or > 2) return "cardholders must contain one or two items";
        if (!request.Cardholders.Any(c => c.CardholderType == CardholderTypeEnum.Primary)) return "Exactly one Primary cardholder is required";
        if (request.Cardholders.Count(c => c.CardholderType == CardholderTypeEnum.Primary) != 1) return "Exactly one Primary cardholder is required";
        foreach (var c in request.Cardholders)
        {
            var photoCount = new[] { c.CardholderPhotoRRSKey, c.CardholderPhotoURL, c.CardholderPhotoKey }.Count(x => !string.IsNullOrWhiteSpace(x));
            if (photoCount != 1) return "Each cardholder requires exactly one photo detail";
        }
        return null;
    }
}