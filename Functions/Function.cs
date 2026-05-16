using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using DemoTravelcardsLambda.Models;
using DemoTravelcardsLambda.Services;
using Npgsql;
using Npgsql.NameTranslation;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace DemoTravelcardsLambda;

public sealed class Function
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
        _service = new Service(BuildDataSource());
    }

    public async Task<APIGatewayProxyResponse> HandleAsync(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var clientId = GetHeaderValue(request.Headers, "client_id");
        try
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return BuildError(HttpStatusCode.BadRequest, "Missing required header 'client_id'.");
            }

            if (string.IsNullOrWhiteSpace(request.Body))
            {
                return BuildError(HttpStatusCode.BadRequest, "Request body is required.");
            }

            if (!ValidateContentType(request.Headers))
            {
                return BuildError(HttpStatusCode.BadRequest, "Invalid or missing Content-Type header. Expected application/json.");
            }

            var requestModel = JsonSerializer.Deserialize<CreateTravelcardRequest>(request.Body, JsonOptions);
            if (requestModel is null)
            {
                return BuildError(HttpStatusCode.BadRequest, "Invalid request body.");
            }

            var validationError = ValidateRequest(requestModel);
            if (validationError is not null)
            {
                return BuildError(HttpStatusCode.BadRequest, validationError);
            }

            var result = await _service.CreateAsync(requestModel);
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
                Body = JsonSerializer.Serialize(result, JsonOptions)
            };
        }
        catch (Exception)
        {
            return BuildError(HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static NpgsqlDataSource BuildDataSource()
    {
        var builder = new NpgsqlDataSourceBuilder(BuildConnectionString());
        builder.MapEnum<TravelcardType>("travelcard_type_enum", new NpgsqlNullNameTranslator());
        builder.MapEnum<CardholderType>("cardholder_type_enum", new NpgsqlNullNameTranslator());
        return builder.Build();
    }

    private static string BuildConnectionString()
    {
        var host = SecretsHelper.Get("host", "POSTGRESQLHOST");
        var port = SecretsHelper.Get("port", "POSTGRESQLPORT");
        var dbname = SecretsHelper.Get("dbname", "POSTGRESQLDATABASE");
        var username = SecretsHelper.Get("username", "POSTGRESQLUSERNAME");
        var password = SecretsHelper.Get("password", "POSTGRESQLPASSWORD");

        var b = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = int.TryParse(port, out var parsedPort) ? parsedPort : 5432,
            Database = dbname,
            Username = username,
            Password = password,
            SslMode = SslMode.Require,
            TrustServerCertificate = true
        };

        return b.ConnectionString;
    }

    private static string? GetHeaderValue(IDictionary<string, string>? headers, string headerName)
    {
        if (headers is null) return null;
        foreach (var kv in headers)
        {
            if (string.Equals(kv.Key, headerName, StringComparison.OrdinalIgnoreCase))
            {
                return kv.Value;
            }
        }
        return null;
    }

    private static bool ValidateContentType(IDictionary<string, string>? headers)
    {
        var value = GetHeaderValue(headers, "Content-Type");
        return !string.IsNullOrWhiteSpace(value) && value.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }

    private static APIGatewayProxyResponse BuildError(HttpStatusCode code, string message)
        => new()
        {
            StatusCode = (int)code,
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            Body = JsonSerializer.Serialize(new ErrorResponse(message), JsonOptions)
        };

    private static string? ValidateRequest(CreateTravelcardRequest request)
    {
        if (!Enum.IsDefined(typeof(TravelcardType), request.TravelcardType))
            return "Invalid value for field 'travelcardType'.";
        if (request.TravelcardRequestedDate >= DateTimeOffset.UtcNow)
            return "travelcardRequestedDate must be in the past.";
        if (request.TravelcardValidFrom > request.TravelcardValidTo)
            return "travelcardValidFrom must not be later than travelcardValidTo.";
        if (request.TravelcardValidTo <= DateTimeOffset.UtcNow)
            return "travelcardValidTo must be in the future.";
        if (request.TravelcardValidFrom > DateTimeOffset.UtcNow.AddMonths(1))
            return "travelcardValidFrom must be no later than one calendar month from today.";
        if (request.TravelcardType == TravelcardType.SixteenToSeventeen && request.TravelcardUsableTo is null)
            return "travelcardUsableTo is required for SixteenToSeventeen travelcard type.";
        if (request.TravelcardUsableTo.HasValue && request.TravelcardUsableTo <= DateTimeOffset.UtcNow)
            return "travelcardUsableTo must be in the future.";
        if ((request.TravelcardType is TravelcardType.SixteenToSeventeen or TravelcardType.Veterans) && request.Cardholders.Any(c => c.CardholderType == CardholderType.Secondary))
            return "Secondary cardholder is not allowed for this travelcard type.";
        if (request.Cardholders.Count is < 1 or > 2)
            return "cardholders must contain exactly one Primary and optional one Secondary cardholder.";
        if (request.Cardholders.Count(c => c.CardholderType == CardholderType.Primary) != 1)
            return "Exactly one Primary cardholder is required.";
        if (request.Cardholders.Count(c => c.CardholderType == CardholderType.Secondary) > 1)
            return "Only one Secondary cardholder is allowed.";
        return null;
    }
}
