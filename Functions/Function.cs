using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Npgsql;
using Npgsql.NameTranslation;
using DemoTravelcardsLambda.Models;
using DemoTravelcardsLambda.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace DemoTravelcardsLambda;

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

    internal static NpgsqlDataSource BuildDataSource()
    {
        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = SecretsHelper.Get("host", "POSTGRESQLHOST"),
            Port = int.TryParse(SecretsHelper.Get("port", "POSTGRESQLPORT"), out var port) ? port : 5432,
            Database = SecretsHelper.Get("dbname", "POSTGRESQLDATABASE"),
            Username = SecretsHelper.Get("username", "POSTGRESQLUSERNAME"),
            Password = SecretsHelper.Get("password", "POSTGRESQLPASSWORD")
        };

        var builder = new NpgsqlDataSourceBuilder(csb.ConnectionString);
        builder.MapEnum<TravelcardType>("travelcard_type_enum", new NpgsqlNullNameTranslator());
        builder.MapEnum<CardholderType>("cardholder_type_enum", new NpgsqlNullNameTranslator());
        return builder.Build();
    }

    public async Task<APIGatewayProxyResponse> DemoTravelcards(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var clientId = GetHeader(request.Headers, "client_id");
        var method = request.HttpMethod ?? string.Empty;
        var route = request.Path ?? string.Empty;
        context.Logger.LogLine($"Controller entry: method={method}, route={route}, client_id={Mask(clientId)}");

        try
        {
            if (string.IsNullOrWhiteSpace(clientId) || clientId.Length < 1 || clientId.Length > 128 || !System.Text.RegularExpressions.Regex.IsMatch(clientId, @"^[\w+]+$"))
                return Error(400, "Invalid or missing header 'client_id'.");

            var contentType = GetHeader(request.Headers, "Content-Type");
            if (!string.IsNullOrWhiteSpace(request.Body) && (string.IsNullOrWhiteSpace(contentType) || !contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase)))
                return Error(400, "Missing or invalid Content-Type header.");

            if (string.IsNullOrWhiteSpace(request.Body))
                return Error(400, "Request body is required.");

            var model = JsonSerializer.Deserialize<RequestDto>(request.Body, JsonOptions);
            if (model is null)
                return Error(400, "Invalid JSON payload.");

            var validationError = Validate(model);
            if (validationError is not null)
                return Error(400, validationError);

            var result = await _service.CreateAsync(model, context);
            context.Logger.LogLine($"Response: generated id={result.TravelcardId}");
            return new APIGatewayProxyResponse
            {
                StatusCode = 201,
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
                Body = JsonSerializer.Serialize(result, JsonOptions)
            };
        }
        catch (Exception)
        {
            return Error(500, "An unexpected error occurred.");
        }
    }

    private static string? Validate(RequestDto request)
    {
        if (!Enum.IsDefined(typeof(TravelcardType), request.TravelcardType))
            return InvalidEnum(nameof(request.TravelcardType), Enum.GetNames(typeof(TravelcardType)));
        if (request.TravelcardValidFrom > DateTimeOffset.UtcNow.AddMonths(1))
            return Fail(nameof(request.TravelcardValidFrom), "must be no later than one calendar month from today.");
        if (request.TravelcardValidFrom > request.TravelcardValidTo)
            return Fail(nameof(request.TravelcardValidFrom), "must not be later than travelcardValidTo.");
        if (request.TravelcardValidTo <= DateTimeOffset.UtcNow)
            return Fail(nameof(request.TravelcardValidTo), "must be in the future.");
        if (request.TravelcardRequestedDate >= DateTimeOffset.UtcNow)
            return Fail(nameof(request.TravelcardRequestedDate), "must be in the past.");
        if (request.TravelcardType == TravelcardType.SixteenToSeventeen && request.TravelcardUsableTo is null)
            return Fail(nameof(request.TravelcardUsableTo), "is required for SixteenToSeventeen travelcards.");
        if (request.TravelcardUsableTo is not null && request.TravelcardUsableTo <= DateTimeOffset.UtcNow)
            return Fail(nameof(request.TravelcardUsableTo), "must be in the future.");
        if (request.Cardholders is null || request.Cardholders.Count < 1 || request.Cardholders.Count > 2)
            return Fail(nameof(request.Cardholders), "must contain exactly one primary and optional one secondary cardholder.");
        if (!request.Cardholders.Any(x => x.CardholderType == CardholderType.Primary) || request.Cardholders.Count(x => x.CardholderType == CardholderType.Primary) != 1)
            return Fail(nameof(request.Cardholders), "must contain exactly one Primary cardholder.");
        if (request.Cardholders.Count(x => x.CardholderType == CardholderType.Secondary) > 1)
            return Fail(nameof(request.Cardholders), "only one Secondary cardholder is allowed.");
        if ((request.TravelcardType == TravelcardType.SixteenToSeventeen || request.TravelcardType == TravelcardType.Veterans) && request.Cardholders.Any(x => x.CardholderType == CardholderType.Secondary))
            return Fail(nameof(request.Cardholders), "secondary cardholder is not allowed for this travelcard type.");
        foreach (var c in request.Cardholders)
        {
            if (!Enum.IsDefined(typeof(CardholderType), c.CardholderType))
                return InvalidEnum(nameof(c.CardholderType), Enum.GetNames(typeof(CardholderType)));
            if (string.IsNullOrWhiteSpace(c.CardholderTitle) || c.CardholderTitle.Length > 15)
                return Fail(nameof(c.CardholderTitle), "must be between 1 and 15 characters.");
            if (string.IsNullOrWhiteSpace(c.CardholderForename) || c.CardholderForename.Length > 100)
                return Fail(nameof(c.CardholderForename), "must be between 1 and 100 characters.");
            if (string.IsNullOrWhiteSpace(c.CardholderSurname) || c.CardholderSurname.Length > 100)
                return Fail(nameof(c.CardholderSurname), "must be between 1 and 100 characters.");
            if (string.IsNullOrWhiteSpace(c.CardholderPhotoName) || c.CardholderPhotoName.Length > 100)
                return Fail(nameof(c.CardholderPhotoName), "must be between 1 and 100 characters.");
            var photoCount = new[] { c.CardholderPhotoRRSKey, c.CardholderPhotoURL, c.CardholderPhotoKey }.Count(x => !string.IsNullOrWhiteSpace(x));
            if (photoCount != 1)
                return Fail("cardholder photo field", "exactly one of cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key is required.");
        }
        return null;
    }

    private static string? InvalidEnum(string field, string[] accepted) => $"Invalid value for field '{field}'. Accepted values: {string.Join(", ", accepted)}";
    private static string Fail(string field, string reason) => $"Validation failed for {field}: {reason}";
    private static APIGatewayProxyResponse Error(int status, string message) => new() { StatusCode = status, Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }, Body = JsonSerializer.Serialize(new { message }, JsonOptions) };
    private static string? GetHeader(IDictionary<string, string>? headers, string name) => headers?.FirstOrDefault(x => string.Equals(x.Key, name, StringComparison.OrdinalIgnoreCase)).Value;
    private static string Mask(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : new string('*', Math.Min(value.Length, 4));
}