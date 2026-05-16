using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using DemotravelcardLambda.Models;
using DemotravelcardLambda.Services;
using Npgsql;
using Npgsql.NameTranslation;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace DemotravelcardLambda;

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

    public async Task<APIGatewayProxyResponse> demotravelcard(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var clientId = GetHeaderValue(request.Headers, "client_id");
        context.Logger.LogLine($"Controller entry: {request.HttpMethod} {request.Path} client_id={clientId}");

        try
        {
            context.Logger.LogLine("Validating request...");
            var validationError = ValidateRequest(request, out var body);
            if (validationError is not null)
            {
                context.Logger.LogLine(validationError.LogMessage);
                return BuildResponse((int)HttpStatusCode.BadRequest, validationError.Message);
            }
            context.Logger.LogLine("Validation passed.");

            var result = await _service.CreateTravelcardAsync(body!, context);
            context.Logger.LogLine($"Response: generated ID {result.TravelcardId}");
            return BuildResponse((int)HttpStatusCode.Created, result);
        }
        catch (JsonException ex)
        {
            context.Logger.LogLine($"Exception: invalid JSON payload. Context=demotravelcard Error={ex.Message}");
            return BuildResponse((int)HttpStatusCode.BadRequest, new { message = "Invalid JSON payload." });
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Exception: Context=demotravelcard Error={ex.Message}");
            return BuildResponse((int)HttpStatusCode.InternalServerError, new { message = "An unexpected error occurred." });
        }
    }

    private static NpgsqlDataSource BuildDataSource()
    {
        var connString = BuildConnectionString();
        var builder = new NpgsqlDataSourceBuilder(connString);
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
            SslMode = SslMode.Require,
            TrustServerCertificate = true
        };
        return csb.ConnectionString;
    }

    private static string? GetHeaderValue(IDictionary<string, string>? headers, string name)
    {
        if (headers is null) return null;
        return headers.FirstOrDefault(kv => string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase)).Value;
    }

    private static ValidationResult? ValidateRequest(APIGatewayProxyRequest request, out Request? body)
    {
        body = null;
        if (string.IsNullOrWhiteSpace(GetHeaderValue(request.Headers, "client_id")))
        {
            return new ValidationResult("Validation failed: client_id missing or invalid.", "client_id missing or invalid.");
        }

        if (!HasJsonContentType(request.Headers))
        {
            return new ValidationResult("Validation failed: Content-Type must contain application/json.", "Content-Type must contain application/json.");
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return new ValidationResult("Validation failed: request body missing.", "request body missing.");
        }

        body = JsonSerializer.Deserialize<Request>(request.Body, JsonOptions);
        if (body is null)
        {
            return new ValidationResult("Validation failed: request body missing.", "request body missing.");
        }

        if (!Enum.IsDefined(typeof(TravelcardType), body.TravelcardType))
        {
            return new ValidationResult("Validation failed: travelcardType invalid enum value. Accepted: [Young, TwoTogether, Family, Senior, Network, TwentySixToThirty, SixteenToSeventeen, Veterans]", "travelcardType invalid enum value. Accepted: [Young, TwoTogether, Family, Senior, Network, TwentySixToThirty, SixteenToSeventeen, Veterans]");
        }

        if (body.TravelcardRequestedDate >= DateTimeOffset.UtcNow)
        {
            return new ValidationResult("Validation failed: travelcardRequestedDate must be in the past.", "travelcardRequestedDate must be in the past.");
        }

        if (body.TravelcardValidFrom > body.TravelcardValidTo)
        {
            return new ValidationResult("Validation failed: travelcardValidFrom must be later than travelcardValidTo.", "travelcardValidFrom must be later than travelcardValidTo.");
        }

        if (body.TravelcardValidTo <= DateTimeOffset.UtcNow)
        {
            return new ValidationResult("Validation failed: travelcardValidTo must be in the future.", "travelcardValidTo must be in the future.");
        }

        if (body.TravelcardType == TravelcardType.SixteenToSeventeen)
        {
            if (body.TravelcardUsableTo is null)
            {
                return new ValidationResult("Validation failed: travelcardUsableTo required for SixteenToSeventeen.", "travelcardUsableTo required for SixteenToSeventeen.");
            }
            if (body.TravelcardUsableTo <= DateTimeOffset.UtcNow)
            {
                return new ValidationResult("Validation failed: travelcardUsableTo must be in the future.", "travelcardUsableTo must be in the future.");
            }
        }
        else if (body.TravelcardUsableTo is not null)
        {
            return new ValidationResult("Validation failed: travelcardUsableTo only allowed for SixteenToSeventeen.", "travelcardUsableTo only allowed for SixteenToSeventeen.");
        }

        if (body.Cardholders is null || body.Cardholders.Count < 1 || body.Cardholders.Count > 2)
        {
            return new ValidationResult("Validation failed: cardholders must contain 1 or 2 items.", "cardholders must contain 1 or 2 items.");
        }

        if (body.Cardholders.Count(c => c.CardholderType == CardholderType.Primary) != 1)
        {
            return new ValidationResult("Validation failed: exactly one Primary cardholder required.", "exactly one Primary cardholder required.");
        }

        if (body.Cardholders.Count(c => c.CardholderType == CardholderType.Secondary) > 1)
        {
            return new ValidationResult("Validation failed: only one Secondary cardholder allowed.", "only one Secondary cardholder allowed.");
        }

        if (body.Cardholders.Any(c => c.CardholderType == CardholderType.Secondary) && !IsSecondaryAllowed(body.TravelcardType))
        {
            return new ValidationResult("Validation failed: secondary cardholder not allowed for travelcard type.", "secondary cardholder not allowed for travelcard type.");
        }

        foreach (var c in body.Cardholders)
        {
            if (!Enum.IsDefined(typeof(CardholderType), c.CardholderType))
            {
                return new ValidationResult("Validation failed: cardholderType invalid enum value. Accepted: [Primary, Secondary]", "cardholderType invalid enum value. Accepted: [Primary, Secondary]");
            }
        }

        return null;
    }

    private static bool HasJsonContentType(IDictionary<string, string>? headers)
        => headers is not null && headers.Any(kv => string.Equals(kv.Key, "Content-Type", StringComparison.OrdinalIgnoreCase) && kv.Value.Contains("application/json", StringComparison.OrdinalIgnoreCase));

    private static bool IsSecondaryAllowed(TravelcardType type)
        => type is TravelcardType.TwoTogether or TravelcardType.Family;

    private static APIGatewayProxyResponse BuildResponse(int statusCode, object body)
        => new()
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            Body = JsonSerializer.Serialize(body, JsonOptions)
        };

    private sealed record ValidationResult(string Message, string LogMessage);
}