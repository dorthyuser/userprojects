using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Npgsql;
using Npgsql.NameTranslation;
using Demo_projectLambda.Models;
using Demo_projectLambda.Services;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Demo_projectLambda;

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
        _service = new Service(BuildDataSource());
    }

    public async Task<APIGatewayProxyResponse> demo_project(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var clientId = GetHeaderValue(request.Headers, "client_id");
        context.Logger.LogLine($"Controller entry: method={request.HttpMethod}, route={request.Path}, client_id={clientId}");

        try
        {
            context.Logger.LogLine("Validating request...");
            if (string.IsNullOrWhiteSpace(clientId) || clientId.Length < 1 || clientId.Length > 128)
            {
                context.Logger.LogLine("Validation failure: client_id - missing or invalid length");
                return Error(HttpStatusCode.BadRequest, "client_id is required and must be between 1 and 128 characters.");
            }

            var body = string.IsNullOrWhiteSpace(request.Body) ? null : JsonSerializer.Deserialize<Request>(request.Body, JsonOptions);
            if (body is null)
            {
                context.Logger.LogLine("Validation failure: request body - invalid or missing");
                return Error(HttpStatusCode.BadRequest, "Invalid request body.");
            }

            var validationError = ValidateRequest(body, out var acceptedEnumError);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                context.Logger.LogLine(validationError);
                return Error(HttpStatusCode.BadRequest, acceptedEnumError ?? validationError);
            }

            context.Logger.LogLine("Validation passed.");

            var result = await _service.CreateTravelcardAsync(body, context);
            context.Logger.LogLine($"Response: generated ID={result.TravelcardId}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json"
                },
                Body = JsonSerializer.Serialize(result, JsonOptions)
            };
        }
        catch (ArgumentException ex)
        {
            context.Logger.LogLine($"Exception: {ex.Message} | context=create travelcard");
            return Error(HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Exception: {ex.Message} | context=create travelcard");
            return Error(HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static NpgsqlDataSource BuildDataSource()
    {
        var cs = BuildConnectionString();
        var builder = new NpgsqlDataSourceBuilder(cs);
        builder.MapEnum<TravelcardType>("travelcard_type_enum", new NpgsqlNullNameTranslator());
        builder.MapEnum<CardholderType>("cardholder_type_enum", new NpgsqlNullNameTranslator());
        return builder.Build();
    }

    private static string BuildConnectionString()
    {
        var host = SecretsHelper.Get("host", "POSTGRESQLHOST");
        var port = SecretsHelper.Get("port", "POSTGRESQLPORT");
        var dbName = SecretsHelper.Get("dbname", "POSTGRESQLDATABASE");
        var username = SecretsHelper.Get("username", "POSTGRESQLUSERNAME");
        var password = SecretsHelper.Get("password", "POSTGRESQLPASSWORD");

        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = int.TryParse(port, out var p) ? p : 5432,
            Database = dbName,
            Username = username,
            Password = password,
            Pooling = true,
            IncludeErrorDetail = false,
            TrustServerCertificate = true
        };
        return csb.ConnectionString;
    }

    private static string? GetHeaderValue(IDictionary<string, string>? headers, string key)
    {
        if (headers is null) return null;
        return headers.FirstOrDefault(h => string.Equals(h.Key, key, StringComparison.OrdinalIgnoreCase)).Value;
    }

    private static string? ValidateRequest(Request request, out string? acceptedEnumError)
    {
        acceptedEnumError = null;

        if (!Enum.TryParse<TravelcardType>(request.TravelcardTypeRaw, true, out var parsedTravelcardType) || !Enum.IsDefined(typeof(TravelcardType), parsedTravelcardType) || !string.Equals(request.TravelcardTypeRaw, parsedTravelcardType.ToString(), StringComparison.Ordinal))
        {
            acceptedEnumError = "Invalid value for field 'travelcardType'. Accepted values: Young, Barcklays, DevonandCornwall, TwoTogether, Family, Senior, DisabledPersons, Network, TwentySixToThirty, SixteenToSeventeen, Veterans";
            return "Validation failure: travelcardType - invalid enum value. Accepted: [Young, Barcklays, DevonandCornwall, TwoTogether, Family, Senior, DisabledPersons, Network, TwentySixToThirty, SixteenToSeventeen, Veterans]";
        }

        if (request.TravelcardRequestedDate >= DateTimeOffset.UtcNow)
        {
            return "Validation failure: travelcardRequestedDate - requested date must be in the past";
        }

        if (request.TravelcardValidFrom > request.TravelcardValidTo)
        {
            return "Validation failure: travelcardValidFrom - valid_from date is later than valid_to date";
        }

        if (request.TravelcardValidTo <= DateTimeOffset.UtcNow)
        {
            return "Validation failure: travelcardValidTo - valid_to date must be in the future";
        }

        if (request.TravelcardValidFrom > DateTimeOffset.UtcNow.AddMonths(1))
        {
            return "Validation failure: travelcardValidFrom - valid_from must be no later than one calendar month from today";
        }

        if (parsedTravelcardType == TravelcardType.SixteenToSeventeen && request.TravelcardUsableTo is null)
        {
            return "Validation failure: travelcardUsableTo - required for SixteenToSeventeen travelcard type";
        }

        if (request.TravelcardUsableTo is not null && request.TravelcardUsableTo <= DateTimeOffset.UtcNow)
        {
            return "Validation failure: travelcardUsableTo - usable_to date must be in the future";
        }

        if ((parsedTravelcardType == TravelcardType.SixteenToSeventeen || parsedTravelcardType == TravelcardType.Veterans) && request.Cardholders.Any(c => c.CardholderType == CardholderType.Secondary))
        {
            return "Validation failure: cardholders - secondary cardholder is not allowed for this travelcard type";
        }

        if (request.Cardholders.Count < 1 || request.Cardholders.Count > 2)
        {
            return "Validation failure: cardholders - must contain exactly one or two items";
        }

        if (request.Cardholders.Count(c => c.CardholderType == CardholderType.Primary) != 1)
        {
            return "Validation failure: cardholders - exactly one primary cardholder is required";
        }

        foreach (var cardholder in request.Cardholders)
        {
            if (string.IsNullOrWhiteSpace(cardholder.CardholderPhotoRRSKey) && string.IsNullOrWhiteSpace(cardholder.CardholderPhotoURL) && string.IsNullOrWhiteSpace(cardholder.CardholderPhotoKey))
            {
                return "Validation failure: cardholder photo - one photo field is required";
            }

            if (!string.IsNullOrWhiteSpace(cardholder.CardholderPhotoURL) && !Uri.IsWellFormedUriString(cardholder.CardholderPhotoURL, UriKind.Absolute))
            {
                return "Validation failure: cardholderPhotoURL - invalid URI";
            }
        }

        return null;
    }

    private static APIGatewayProxyResponse Error(HttpStatusCode statusCode, string message)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            },
            Body = JsonSerializer.Serialize(new { message }, JsonOptions)
        };
    }
}