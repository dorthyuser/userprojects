using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Npgsql;
using Npgsql.NameTranslation;
using Travelcardcsharp1008Lambda.Models;
using Travelcardcsharp1008Lambda.Services;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Travelcardcsharp1008Lambda;

public class Function
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(null, allowIntegerValues: false) }
    };

    private readonly Service _service;

    public Function()
    {
        var dataSource = BuildDataSource();
        _service = new Service(dataSource);
    }

    public async Task<APIGatewayProxyResponse> travelcardcsharp1008(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var clientId = GetHeader(request.Headers, "client_id");
        context.Logger.LogLine($"Controller entry: {request.HttpMethod} {request.Path} client_id={clientId}");

        try
        {
            var body = request.Body ?? string.Empty;
            var model = JsonSerializer.Deserialize<Request>(body, JsonOptions);
            if (model is null)
            {
                return Error(HttpStatusCode.BadRequest, "Invalid request body.");
            }

            context.Logger.LogLine("Validating request...");
            var validationError = ValidateRequest(model);
            if (validationError is not null)
            {
                context.Logger.LogLine(validationError);
                return Error(HttpStatusCode.BadRequest, validationError);
            }
            context.Logger.LogLine("Validation passed.");

            var response = await _service.CreateTravelcardAsync(model, context);
            context.Logger.LogLine($"Response: generated ID {response.TravelcardId}");

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
                Body = JsonSerializer.Serialize(response, JsonOptions)
            };
        }
        catch (JsonException ex)
        {
            context.Logger.LogLine($"Exception: {ex.Message} context=JSON deserialization");
            return Error(HttpStatusCode.BadRequest, "Invalid JSON payload.");
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Exception: {ex.Message} context=Create travelcard");
            return Error(HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static NpgsqlDataSource BuildDataSource()
    {
        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = SecretsHelper.Get("host", "POSTGRESQLHOST"),
            Port = int.TryParse(SecretsHelper.Get("port", "POSTGRESQLPORT"), out var port) ? port : 5432,
            Database = SecretsHelper.Get("dbname", "POSTGRESQLDATABASE"),
            Username = SecretsHelper.Get("username", "POSTGRESQLUSERNAME"),
            Password = SecretsHelper.Get("password", "POSTGRESQLPASSWORD"),
            SslMode = SslMode.Require,
            TrustServerCertificate = true
        };

        var builder = new NpgsqlDataSourceBuilder(csb.ConnectionString);
        builder.MapEnum<CardholderTypeEnum>("cardholder_type_enum", new NpgsqlNullNameTranslator());
        builder.MapEnum<TravelcardTypeEnum>("travelcard_type_enum", new NpgsqlNullNameTranslator());
        return builder.Build();
    }

    private static string? GetHeader(IDictionary<string, string>? headers, string key)
    {
        if (headers is null) return null;
        return headers.FirstOrDefault(h => string.Equals(h.Key, key, StringComparison.OrdinalIgnoreCase)).Value;
    }

    private static APIGatewayProxyResponse Error(HttpStatusCode statusCode, string message)
    {
        var payload = new { message };
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            Body = JsonSerializer.Serialize(payload, JsonOptions)
        };
    }

    private static string? ValidateRequest(Request request)
    {
        if (!Enum.IsDefined(typeof(TravelcardTypeEnum), request.TravelcardType))
            return $"Invalid value for field 'travelcardType'. Accepted values: {string.Join(", ", Enum.GetNames(typeof(TravelcardTypeEnum)))}";

        if (request.TravelcardRequestedDate >= DateTimeOffset.UtcNow)
            return "travelcardRequestedDate must be in the past.";

        if (request.TravelcardValidFrom > request.TravelcardValidTo)
            return "travelcardValidFrom must not be later than travelcardValidTo.";

        if (request.TravelcardValidTo <= DateTimeOffset.UtcNow)
            return "travelcardValidTo must be in the future.";

        if (request.TravelcardValidFrom > DateTimeOffset.UtcNow.AddMonths(1))
            return "travelcardValidFrom must not be later than one calendar month from today.";

        if (request.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen && request.TravelcardUsableTo is null)
            return "travelcardUsableTo is required for SixteenToSeventeen travelcard type.";

        if (request.TravelcardUsableTo is not null && request.TravelcardUsableTo <= DateTimeOffset.UtcNow)
            return "travelcardUsableTo must be in the future.";

        if ((request.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen || request.TravelcardType == TravelcardTypeEnum.Veterans) && request.Cardholders.Any(c => c.CardholderType == CardholderTypeEnum.Secondary))
            return "Secondary cardholder is not allowed for this travelcard type.";

        if (request.Cardholders.Count < 1 || request.Cardholders.Count > 2)
            return "cardholders must contain exactly one or two items.";

        if (request.Cardholders.Count(c => c.CardholderType == CardholderTypeEnum.Primary) != 1)
            return "Exactly one Primary cardholder is required.";

        foreach (var cardholder in request.Cardholders)
        {
            if (string.IsNullOrWhiteSpace(cardholder.CardholderTitle) || cardholder.CardholderTitle.Length > 15)
                return "Invalid cardholderTitle.";
            if (string.IsNullOrWhiteSpace(cardholder.CardholderForename) || cardholder.CardholderForename.Length > 100)
                return "Invalid cardholderForename.";
            if (string.IsNullOrWhiteSpace(cardholder.CardholderSurname) || cardholder.CardholderSurname.Length > 100)
                return "Invalid cardholderSurname.";
            if (string.IsNullOrWhiteSpace(cardholder.CardholderPhotoName) || cardholder.CardholderPhotoName.Length > 100)
                return "Invalid cardholderPhotoName.";

            var photoFields = new[] { cardholder.CardholderPhotoRRSKey, cardholder.CardholderPhotoURL, cardholder.CardholderPhotoKey }.Count(v => !string.IsNullOrWhiteSpace(v));
            if (photoFields != 1)
                return "Each cardholder must provide exactly one photo detail field.";
        }

        return null;
    }
}