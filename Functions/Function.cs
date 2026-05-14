using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Npgsql;
using Npgsql.NameTranslation;
using New_project2Lambda.Models;
using New_project2Lambda.Services;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace New_project2Lambda;

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

    public async Task<APIGatewayProxyResponse> new_project2(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var clientId = GetHeader(request.Headers, "client_id");
        context.Logger.LogLine($"Entry: method={request.HttpMethod}, route={request.Path}, client_id={(string.IsNullOrWhiteSpace(clientId) ? "" : clientId)}");

        if (string.IsNullOrWhiteSpace(clientId))
        {
            return ErrorResponse(HttpStatusCode.BadRequest, "Missing required header 'client_id'.");
        }

        try
        {
            context.Logger.LogLine("Validating request...");
            var model = DeserializeRequest(request.Body, out var deserializeError);
            if (deserializeError is not null)
            {
                return ErrorResponse(HttpStatusCode.BadRequest, deserializeError);
            }

            var validationError = ValidateRequest(model!, context.Logger);
            if (validationError is not null)
            {
                return ErrorResponse(HttpStatusCode.BadRequest, validationError);
            }

            context.Logger.LogLine("Validation passed.");
            var response = await _service.CreateAsync(model!, context.Logger);
            context.Logger.LogLine($"Response generated id={response.TravelcardId}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
                Body = JsonSerializer.Serialize(response, JsonOptions)
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Exception: {ex.Message} | context=create travelcard");
            return ErrorResponse(HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static string? GetHeader(IDictionary<string, string>? headers, string name)
    {
        if (headers is null) return null;
        foreach (var kvp in headers)
        {
            if (string.Equals(kvp.Key, name, StringComparison.OrdinalIgnoreCase)) return kvp.Value;
        }
        return null;
    }

    private static Request? DeserializeRequest(string? body, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(body))
        {
            error = "Request body is required.";
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Request>(body, JsonOptions);
        }
        catch (JsonException je)
        {
            error = je.Message.Contains("TravelcardType", StringComparison.OrdinalIgnoreCase)
                ? "Invalid value for field 'travelcardType'. Accepted values: Young, Barcklays, DevonandCornwall, TwoTogether, Family, Senior, DisabledPersons, Network, TwentySixToThirty, SixteenToSeventeen, Veterans"
                : "Invalid request payload.";
            return null;
        }
    }

    private static string? ValidateRequest(Request request, ILambdaLogger logger)
    {
        if (!Enum.IsDefined(typeof(TravelcardType), request.TravelcardType))
        {
            logger.LogLine("Validation failure: travelcardType invalid enum value. Accepted: [Young, Barcklays, DevonandCornwall, TwoTogether, Family, Senior, DisabledPersons, Network, TwentySixToThirty, SixteenToSeventeen, Veterans]");
            return "Invalid value for field 'travelcardType'. Accepted values: Young, Barcklays, DevonandCornwall, TwoTogether, Family, Senior, DisabledPersons, Network, TwentySixToThirty, SixteenToSeventeen, Veterans";
        }

        if (request.TravelcardRequestedDate >= DateTimeOffset.UtcNow)
        {
            logger.LogLine("Validation failure: travelcardRequestedDate must be in the past.");
            return "Invalid value for field 'travelcardRequestedDate'.";
        }

        if (request.TravelcardValidFrom > request.TravelcardValidTo)
        {
            logger.LogLine("Validation failure: travelcardValidFrom later than travelcardValidTo.");
            return "Invalid value for field 'travelcardValidFrom'.";
        }

        if (request.TravelcardValidTo <= DateTimeOffset.UtcNow)
        {
            logger.LogLine("Validation failure: travelcardValidTo must be in the future.");
            return "Invalid value for field 'travelcardValidTo'.";
        }

        if (request.TravelcardValidFrom > DateTimeOffset.UtcNow.AddMonths(1))
        {
            logger.LogLine("Validation failure: travelcardValidFrom later than one calendar month.");
            return "Invalid value for field 'travelcardValidFrom'.";
        }

        if (request.TravelcardType == TravelcardType.SixteenToSeventeen)
        {
            if (!request.TravelcardUsableTo.HasValue)
            {
                logger.LogLine("Validation failure: travelcardUsableTo required for SixteenToSeventeen.");
                return "Invalid value for field 'travelcardUsableTo'.";
            }
        }
        else if (request.TravelcardUsableTo.HasValue && request.TravelcardUsableTo <= DateTimeOffset.UtcNow)
        {
            logger.LogLine("Validation failure: travelcardUsableTo must be in the future.");
            return "Invalid value for field 'travelcardUsableTo'.";
        }

        if (request.Cardholders.Count is < 1 or > 2)
        {
            logger.LogLine("Validation failure: cardholders count must be 1 or 2.");
            return "Invalid value for field 'cardholders'.";
        }

        if (request.Cardholders.Count(c => c.CardholderType == CardholderType.Primary) != 1)
        {
            logger.LogLine("Validation failure: exactly one primary cardholder required.");
            return "Invalid value for field 'cardholders'.";
        }

        if (request.Cardholders.Any(c => c.CardholderType == CardholderType.Secondary) && request.TravelcardType is TravelcardType.SixteenToSeventeen or TravelcardType.Veterans)
        {
            logger.LogLine("Validation failure: secondary cardholder not allowed for travelcard type.");
            return "Invalid value for field 'cardholders'.";
        }

        foreach (var cardholder in request.Cardholders)
        {
            if (string.IsNullOrWhiteSpace(cardholder.CardholderTitle) || cardholder.CardholderTitle.Length > 15)
            {
                logger.LogLine("Validation failure: cardholderTitle invalid.");
                return "Invalid value for field 'cardholderTitle'.";
            }
            if (string.IsNullOrWhiteSpace(cardholder.CardholderForename) || cardholder.CardholderForename.Length > 100)
            {
                logger.LogLine("Validation failure: cardholderForename invalid.");
                return "Invalid value for field 'cardholderForename'.";
            }
            if (string.IsNullOrWhiteSpace(cardholder.CardholderSurname) || cardholder.CardholderSurname.Length > 100)
            {
                logger.LogLine("Validation failure: cardholderSurname invalid.");
                return "Invalid value for field 'cardholderSurname'.";
            }
            if (string.IsNullOrWhiteSpace(cardholder.CardholderPhotoName) || cardholder.CardholderPhotoName.Length > 100)
            {
                logger.LogLine("Validation failure: cardholderPhotoName invalid.");
                return "Invalid value for field 'cardholderPhotoName'.";
            }
            var imageCount = new[] { cardholder.CardholderPhotoRRSKey, cardholder.CardholderPhotoURL, cardholder.CardholderPhotoKey }.Count(x => !string.IsNullOrWhiteSpace(x));
            if (imageCount != 1)
            {
                logger.LogLine("Validation failure: one photo detail required.");
                return "Invalid value for field 'cardholders'.";
            }
        }

        return null;
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

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = int.TryParse(port, out var p) ? p : 5432,
            Database = dbName,
            Username = username,
            Password = password,
            SslMode = SslMode.Require,
            TrustServerCertificate = true
        };
        return builder.ConnectionString;
    }

    private static APIGatewayProxyResponse ErrorResponse(HttpStatusCode statusCode, string message) => new()
    {
        StatusCode = (int)statusCode,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = JsonSerializer.Serialize(new { message }, JsonOptions)
    };
}