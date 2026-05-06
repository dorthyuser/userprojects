using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Travelcardcsharplambda355Lambda.Enums;
using Travelcardcsharplambda355Lambda.Models;
using Travelcardcsharplambda355Lambda.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Travelcardcsharplambda355Lambda;

public class Function
{
    private readonly Service _service;
    private readonly JsonSerializerOptions _jsonOptions;

    public Function()
    {
        _service = new Service();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
    }

    public async Task<APIGatewayProxyResponse> travelcardcsharplambda355(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            if (request.Headers == null || !request.Headers.TryGetValue("client_id", out var clientId) || string.IsNullOrWhiteSpace(clientId))
            {
                return BuildError(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "client_id is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Body))
            {
                return BuildError(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "Request body is required.");
            }

            var model = JsonSerializer.Deserialize<Request>(request.Body, _jsonOptions);
            if (model == null)
            {
                return BuildError(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "Invalid request body.");
            }

            var validationError = Validate(model);
            if (validationError != null)
            {
                return BuildError(HttpStatusCode.BadRequest, "VALIDATION_ERROR", validationError);
            }

            var result = await _service.CreateTravelcardAsync(model);
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
                Body = JsonSerializer.Serialize(result, _jsonOptions)
            };
        }
        catch
        {
            return BuildError(HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.");
        }
    }

    private static string? Validate(Request request)
    {
        var now = DateTimeOffset.UtcNow;

        if (request.TravelcardRequestedDate >= now)
        {
            return "travelcardRequestedDate must be in the past.";
        }

        if (request.TravelcardValidFrom > request.TravelcardValidTo)
        {
            return "travelcardValidFrom must not be later than travelcardValidTo.";
        }

        if (request.TravelcardValidTo <= now)
        {
            return "travelcardValidTo must be in the future.";
        }

        if (request.TravelcardValidFrom > now.AddMonths(1))
        {
            return "travelcardValidFrom must be no later than one calendar month from today.";
        }

        if (request.TravelcardType == TravelcardType.SixteenToSeventeen && request.TravelcardUsableTo == null)
        {
            return "travelcardUsableTo is required for SixteenToSeventeen travelcards.";
        }

        if (request.TravelcardUsableTo.HasValue && request.TravelcardUsableTo.Value <= now)
        {
            return "travelcardUsableTo must be in the future.";
        }

        if (request.Cardholders == null || request.Cardholders.Count is < 1 or > 2)
        {
            return "cardholders must contain exactly one or two items.";
        }

        if (request.Cardholders.Count(c => c.CardholderType == CardholderType.Primary) != 1)
        {
            return "Exactly one Primary cardholder is required.";
        }

        if ((request.TravelcardType == TravelcardType.SixteenToSeventeen || request.TravelcardType == TravelcardType.Veterans) && request.Cardholders.Any(c => c.CardholderType == CardholderType.Secondary))
        {
            return "Secondary cardholder is not allowed for SixteenToSeventeen and Veterans travelcards.";
        }

        return null;
    }

    private static APIGatewayProxyResponse BuildError(HttpStatusCode statusCode, string code, string message)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
            Body = JsonSerializer.Serialize(new
            {
                error = new
                {
                    code,
                    message
                }
            })
        };
    }
}
