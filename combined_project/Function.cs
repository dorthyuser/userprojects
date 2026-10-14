using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using combined_project.Infrastructure;
using combined_project.Models;
using combined_project.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace combined_project;

public sealed class Function
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(null, allowIntegerValues: false) }
    };

    private readonly PrimeService _primeService;
    private readonly TravelcardService _travelcardService;

    public Function()
    {
        var dataSource = DatabaseFactory.CreateDataSource();
        _primeService = new PrimeService(dataSource);
        _travelcardService = new TravelcardService(dataSource);
    }

    public async Task<APIGatewayProxyResponse> HandleAsync(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var path = request.Path ?? string.Empty;
        var method = request.HttpMethod ?? string.Empty;
        context.Logger.LogLine(JsonSerializer.Serialize(new { event = "route_entry", method, path }));

        try
        {
            if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) && path.Equals("/prime", StringComparison.OrdinalIgnoreCase))
            {
                var prime = await _primeService.GetPrimeNumberBetween100And200Async(context);
                return Json(HttpStatusCode.OK, new { prime_number = prime });
            }

            if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) && path.Equals("/travelcards", StringComparison.OrdinalIgnoreCase))
            {
                if (!HasJsonContentType(request.Headers)) return Error(HttpStatusCode.UnsupportedMediaType, "Invalid or missing Content-Type header.");
                if (string.IsNullOrWhiteSpace(request.Body)) return Error(HttpStatusCode.BadRequest, "Request body is required.");
                CreateTravelcardRequest? model;
                try { model = JsonSerializer.Deserialize<CreateTravelcardRequest>(request.Body, JsonOptions); }
                catch { return Error(HttpStatusCode.BadRequest, "Invalid request body."); }
                if (model is null) return Error(HttpStatusCode.BadRequest, "Invalid request body.");
                var validation = ValidateTravelcard(model);
                if (validation is not null) return Error(HttpStatusCode.BadRequest, validation);
                var result = await _travelcardService.CreateAsync(model, context);
                return Json(HttpStatusCode.Created, result);
            }

            return Error(HttpStatusCode.NotFound, "Not Found");
        }
        catch
        {
            return Error(HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static bool HasJsonContentType(IDictionary<string, string>? headers)
    {
        if (headers is null) return false;
        foreach (var kv in headers)
        {
            if (string.Equals(kv.Key, "Content-Type", StringComparison.OrdinalIgnoreCase) && kv.Value.Contains("application/json", StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static string? ValidateTravelcard(CreateTravelcardRequest model)
    {
        if (model.Cardholders is null || model.Cardholders.Count is < 1 or > 2) return "Cardholders must contain 1 or 2 items.";
        if (!model.Cardholders.Any(x => x.CardholderType == CardholderType.Primary)) return "Primary cardholder is required.";
        if (model.Cardholders.Count(x => x.CardholderType == CardholderType.Secondary) > 1) return "Only one secondary cardholder is allowed.";
        if ((model.TravelcardType == TravelcardType.SixteenToSeventeen || model.TravelcardType == TravelcardType.Veterans) && model.Cardholders.Any(x => x.CardholderType == CardholderType.Secondary)) return "Secondary cardholder is not allowed for this travelcard type.";
        if (model.TravelcardType == TravelcardType.SixteenToSeventeen && model.TravelcardUsableTo is null) return "Travelcard usable to is required for SixteenToSeventeen.";
        if (model.TravelcardUsableTo is not null && model.TravelcardUsableTo <= DateTimeOffset.UtcNow) return "Travelcard usable to must be in the future.";
        if (model.TravelcardRequestedDate >= DateTimeOffset.UtcNow) return "Requested date must be in the past.";
        if (model.TravelcardValidFrom > DateTimeOffset.UtcNow.AddMonths(1)) return "Valid from must be no later than one calendar month from today.";
        if (model.TravelcardValidFrom > model.TravelcardValidTo) return "Valid from must be later than valid to.";
        if (model.TravelcardValidTo <= DateTimeOffset.UtcNow) return "Valid to must be in the future.";
        if (string.IsNullOrWhiteSpace(model.TravelcardTransactionReference) || model.TravelcardTransactionReference.Length != 15) return "Transaction reference must be exactly 15 characters.";
        foreach (var ch in model.Cardholders)
        {
            var count = 0;
            if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoRRSKey)) count++;
            if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoURL)) count++;
            if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoKey)) count++;
            if (count != 1) return "Exactly one cardholder photo detail must be provided.";
        }
        return null;
    }

    private static APIGatewayProxyResponse Json(HttpStatusCode statusCode, object body) => new()
    {
        StatusCode = (int)statusCode,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = JsonSerializer.Serialize(body, JsonOptions)
    };

    private static APIGatewayProxyResponse Error(HttpStatusCode statusCode, string message) => Json(statusCode, new ErrorResponse { Message = message });
}
