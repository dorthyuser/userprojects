using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Npgsql;
using Npgsql.NameTranslation;
using TravelcardsDemoLambda.Models;
using TravelcardsDemoLambda.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace TravelcardsDemoLambda;

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

    public async Task<APIGatewayProxyResponse> HandleAsync(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var clientId = GetHeader(request.Headers, "client_id");
        context.Logger.LogLine($"Controller entry: {request.HttpMethod} {request.Path} client_id={Mask(clientId)}");
        try
        {
            if (!string.IsNullOrWhiteSpace(request.Body) && !HasJsonContentType(request.Headers))
            {
                return ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Invalid or missing Content-Type header.");
            }

            var validation = ValidateRequest(request, out var model);
            if (!validation.Success)
            {
                context.Logger.LogLine($"Validation failure: {validation.ErrorField} - {validation.ErrorReason}");
                return ErrorResponse(HttpStatusCode.BadRequest, validation.ErrorMessage);
            }

            var result = await _service.CreateAsync(model!, context);
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
                Body = JsonSerializer.Serialize(result, JsonOptions)
            };
        }
        catch
        {
            return ErrorResponse(HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static NpgsqlDataSource BuildDataSource()
    {
        var cs = BuildConnectionString();
        var csb = new NpgsqlConnectionStringBuilder(cs);
        var builder = new NpgsqlDataSourceBuilder(csb.ConnectionString);
        builder.MapEnum<TravelcardType>("travelcard_type_enum", new NpgsqlNullNameTranslator());
        builder.MapEnum<CardholderType>("cardholder_type_enum", new NpgsqlNullNameTranslator());
        return builder.Build();
    }

    private static string BuildConnectionString()
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = SecretsHelper.Get("host", "POSTGRESQLHOST"),
            Port = int.TryParse(SecretsHelper.Get("port", "POSTGRESQLPORT"), out var port) ? port : 5432,
            Database = SecretsHelper.Get("dbname", "POSTGRESQLDATABASE"),
            Username = SecretsHelper.Get("username", "POSTGRESQLUSERNAME"),
            Password = SecretsHelper.Get("password", "POSTGRESQLPASSWORD")
        };
        return builder.ConnectionString;
    }

    private static string GetHeader(IDictionary<string, string>? headers, string name)
    {
        if (headers == null) return string.Empty;
        foreach (var kv in headers)
        {
            if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase)) return kv.Value;
        }
        return string.Empty;
    }

    private static bool HasJsonContentType(IDictionary<string, string>? headers)
    {
        var value = GetHeader(headers, "Content-Type");
        return !string.IsNullOrWhiteSpace(value) && value.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }

    private static ValidationResult ValidateRequest(APIGatewayProxyRequest request, out CreateTravelcardRequest? model)
    {
        model = null;
        if (string.IsNullOrWhiteSpace(request.Body)) return ValidationResult.Fail("body", "request body is required.", "Request body is required.");
        try
        {
            model = JsonSerializer.Deserialize<CreateTravelcardRequest>(request.Body, JsonOptions);
        }
        catch (JsonException)
        {
            return ValidationResult.Fail("body", "invalid json.", "Invalid request body.");
        }

        if (model == null) return ValidationResult.Fail("body", "invalid json.", "Invalid request body.");
        if (!Enum.IsDefined(typeof(TravelcardType), model.TravelcardType)) return ValidationResult.Fail("travelcardType", $"invalid enum value. Accepted: [{string.Join(", ", Enum.GetNames(typeof(TravelcardType)))}]", $"Invalid value for field 'travelcardType'. Accepted values: {string.Join(", ", Enum.GetNames(typeof(TravelcardType)))}");
        if (model.Cardholders == null || model.Cardholders.Count is < 1 or > 2) return ValidationResult.Fail("cardholders", "must contain one primary and optional one secondary.", "Cardholders must contain 1 or 2 items.");
        if (!model.Cardholders.Any(x => x.CardholderType == CardholderType.Primary)) return ValidationResult.Fail("cardholderType", "missing primary cardholder.", "Primary cardholder is required.");
        if (model.Cardholders.Count(x => x.CardholderType == CardholderType.Secondary) > 1) return ValidationResult.Fail("cardholderType", "multiple secondary cardholders are not allowed.", "Only one secondary cardholder is allowed.");
        if ((model.TravelcardType == TravelcardType.SixteenToSeventeen || model.TravelcardType == TravelcardType.Veterans) && model.Cardholders.Any(x => x.CardholderType == CardholderType.Secondary)) return ValidationResult.Fail("cardholderType", "secondary cardholder is not allowed for this travelcard type.", "Secondary cardholder is not allowed for this travelcard type.");
        if (model.TravelcardType == TravelcardType.SixteenToSeventeen && model.TravelcardUsableTo == null) return ValidationResult.Fail("travelcardUsableTo", "required for SixteenToSeventeen.", "Travelcard usable to is required for SixteenToSeventeen.");
        if (model.TravelcardUsableTo != null && model.TravelcardUsableTo <= DateTimeOffset.UtcNow) return ValidationResult.Fail("travelcardUsableTo", "must be in the future.", "Travelcard usable to must be in the future.");
        if (model.TravelcardRequestedDate >= DateTimeOffset.UtcNow) return ValidationResult.Fail("travelcardRequestedDate", "must be in the past.", "Requested date must be in the past.");
        if (model.TravelcardValidFrom > DateTimeOffset.UtcNow.AddMonths(1)) return ValidationResult.Fail("travelcardValidFrom", "must be within one calendar month from today.", "Valid from must be no later than one calendar month from today.");
        if (model.TravelcardValidFrom > model.TravelcardValidTo) return ValidationResult.Fail("travelcardValidFrom", "cannot be later than valid_to.", "Valid from must be later than valid to.");
        if (model.TravelcardValidTo <= DateTimeOffset.UtcNow) return ValidationResult.Fail("travelcardValidTo", "must be in the future.", "Valid to must be in the future.");
        if (string.IsNullOrWhiteSpace(model.TravelcardTransactionReference) || model.TravelcardTransactionReference.Length != 15) return ValidationResult.Fail("travelcardTransactionReference", "must be exactly 15 characters.", "Transaction reference must be exactly 15 characters.");
        foreach (var ch in model.Cardholders)
        {
            if (!Enum.IsDefined(typeof(CardholderType), ch.CardholderType)) return ValidationResult.Fail("cardholderType", $"invalid enum value. Accepted: [{string.Join(", ", Enum.GetNames(typeof(CardholderType)))}]", $"Invalid value for field 'cardholderType'. Accepted values: {string.Join(", ", Enum.GetNames(typeof(CardholderType)))}");
            if (!HasOnePhotoDetail(ch)) return ValidationResult.Fail("cardholderPhoto", "one of photo fields must be provided.", "Exactly one cardholder photo detail must be provided.");
        }

        return ValidationResult.Ok();
    }

    private static bool HasOnePhotoDetail(CardholderRequest ch)
    {
        var count = 0;
        if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoRRSKey)) count++;
        if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoURL)) count++;
        if (!string.IsNullOrWhiteSpace(ch.CardholderPhotoKey)) count++;
        return count == 1;
    }

    private static string Mask(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : "***";

    private static APIGatewayProxyResponse ErrorResponse(HttpStatusCode statusCode, string message) => new()
    {
        StatusCode = (int)statusCode,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = JsonSerializer.Serialize(new ErrorResponse { Message = message }, JsonOptions)
    };

    private sealed record ValidationResult(bool Success, string ErrorField, string ErrorReason, string ErrorMessage)
    {
        public static ValidationResult Ok() => new(true, string.Empty, string.Empty, string.Empty);
        public static ValidationResult Fail(string field, string reason, string message) => new(false, field, reason, message);
    }
}