using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Npgsql;
using Npgsql.NameTranslation;
using Travelcardcsharp1017Lambda.Models;
using Travelcardcsharp1017Lambda.Services;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Travelcardcsharp1017Lambda;

public class Function
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(null, allowIntegerValues: false) }
    };

    private readonly Service _service;

    public Function() : this(BuildDataSource())
    {
    }

    public Function(NpgsqlDataSource dataSource)
    {
        _service = new Service(dataSource);
    }

    private static NpgsqlDataSource BuildDataSource()
    {
        var connectionString = BuildConnectionString();
        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.MapEnum<TravelcardType>("travelcard_type_enum", new NpgsqlNullNameTranslator());
        builder.MapEnum<CardholderType>("cardholder_type_enum", new NpgsqlNullNameTranslator());
        return builder.Build();
    }

    private static string BuildConnectionString()
    {
        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = SecretsHelper.Get("host", "POSTGRESQLHOST"),
            Port = int.TryParse(SecretsHelper.Get("port", "POSTGRESQLPORT"), out var port) ? port : 5432,
            Database = SecretsHelper.Get("dbname", "POSTGRESQLDATABASE"),
            Username = SecretsHelper.Get("username", "POSTGRESQLUSERNAME"),
            Password = SecretsHelper.Get("password", "POSTGRESQLPASSWORD"),
            Pooling = true,
            IncludeErrorDetail = false
        };
        return csb.ConnectionString;
    }

    public async Task<APIGatewayProxyResponse> travelcardcsharp1017(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var clientId = GetHeader(request.Headers, "client_id");
        context.Logger.LogLine($"Controller entry: method={request.HttpMethod}, route={request.Path}, client_id={clientId}");

        if (string.IsNullOrWhiteSpace(clientId))
        {
            context.Logger.LogLine("Validation failure: client_id missing or blank");
            return CreateResponse(HttpStatusCode.BadRequest, new ErrorResponse("client_id is required."));
        }

        if (!string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
        {
            return CreateResponse(HttpStatusCode.MethodNotAllowed, new ErrorResponse("Method not allowed."));
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return CreateResponse(HttpStatusCode.BadRequest, new ErrorResponse("Request body is required."));
        }

        try
        {
            context.Logger.LogLine("Validating request...");
            var model = JsonSerializer.Deserialize<CreateTravelcardRequest>(request.Body, JsonOptions);
            if (model is null)
            {
                return CreateResponse(HttpStatusCode.BadRequest, new ErrorResponse("Invalid request body."));
            }

            var validationError = ValidateRequest(model);
            if (validationError is not null)
            {
                return CreateResponse(HttpStatusCode.BadRequest, new ErrorResponse(validationError));
            }

            context.Logger.LogLine("Validation passed.");
            context.Logger.LogLine("DB operation: travelcards INSERT");
            var response = await _service.CreateAsync(model, context);
            context.Logger.LogLine($"Response: generated ID {response.TravelcardId}");
            return CreateResponse(HttpStatusCode.Created, response);
        }
        catch (JsonException ex)
        {
            context.Logger.LogLine($"Exception: {ex.Message} | Context: JSON deserialization");
            return CreateResponse(HttpStatusCode.BadRequest, new ErrorResponse("Invalid JSON payload."));
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Exception: {ex.Message} | Context: travelcardcsharp1017");
            return CreateResponse(HttpStatusCode.InternalServerError, new ErrorResponse("An unexpected error occurred."));
        }
    }

    private static string? ValidateRequest(CreateTravelcardRequest request)
    {
        if (!Enum.IsDefined(typeof(TravelcardType), request.TravelcardType))
        {
            return $"Invalid value for field 'travelcardType'. Accepted values: {string.Join(", ", Enum.GetNames(typeof(TravelcardType)))}";
        }

        if (request.TravelcardValidFrom > DateTimeOffset.UtcNow.AddMonths(1))
        {
            return "travelcardValidFrom cannot be later than one calendar month from today.";
        }

        if (request.TravelcardValidFrom > request.TravelcardValidTo)
        {
            return "travelcardValidFrom cannot be later than travelcardValidTo.";
        }

        if (request.TravelcardValidTo <= DateTimeOffset.UtcNow)
        {
            return "travelcardValidTo must be in the future.";
        }

        if (request.TravelcardRequestedDate > DateTimeOffset.UtcNow)
        {
            return "travelcardRequestedDate must be in the past.";
        }

        if (request.TravelcardType == TravelcardType.SixteenToSeventeen)
        {
            if (!request.TravelcardUsableTo.HasValue)
            {
                return "travelcardUsableTo is required for SixteenToSeventeen travelcards.";
            }
        }

        if (request.TravelcardUsableTo.HasValue && request.TravelcardUsableTo.Value <= DateTimeOffset.UtcNow)
        {
            return "travelcardUsableTo must be in the future.";
        }

        if ((request.TravelcardType == TravelcardType.SixteenToSeventeen || request.TravelcardType == TravelcardType.Veterans) && request.Cardholders.Any(c => c.CardholderType == CardholderType.Secondary))
        {
            return "Secondary cardholder is not allowed for the selected travelcard type.";
        }

        if (request.Cardholders.Count is < 1 or > 2)
        {
            return "cardholders must contain one or two items.";
        }

        var primaryCount = request.Cardholders.Count(c => c.CardholderType == CardholderType.Primary);
        if (primaryCount != 1)
        {
            return "Exactly one Primary cardholder is required.";
        }

        if (request.Cardholders.Count(c => c.CardholderType == CardholderType.Secondary) > 1)
        {
            return "Only one Secondary cardholder is allowed.";
        }

        foreach (var cardholder in request.Cardholders)
        {
            if (string.IsNullOrWhiteSpace(cardholder.CardholderTitle) || cardholder.CardholderTitle.Length > 15)
            {
                return "Invalid cardholderTitle.";
            }

            if (string.IsNullOrWhiteSpace(cardholder.CardholderForename) || cardholder.CardholderForename.Length > 100)
            {
                return "Invalid cardholderForename.";
            }

            if (string.IsNullOrWhiteSpace(cardholder.CardholderSurname) || cardholder.CardholderSurname.Length > 100)
            {
                return "Invalid cardholderSurname.";
            }

            if (string.IsNullOrWhiteSpace(cardholder.CardholderPhotoName) || cardholder.CardholderPhotoName.Length > 100)
            {
                return "Invalid cardholderPhotoName.";
            }

            var photoCount = new[] { cardholder.CardholderPhotoRRSKey, cardholder.CardholderPhotoURL, cardholder.CardholderPhotoKey }.Count(x => !string.IsNullOrWhiteSpace(x));
            if (photoCount != 1)
            {
                return "Each cardholder must provide exactly one photo detail field.";
            }
        }

        return null;
    }

    private static string? GetHeader(IDictionary<string, string>? headers, string key)
    {
        if (headers is null) return null;
        return headers.FirstOrDefault(h => string.Equals(h.Key, key, StringComparison.OrdinalIgnoreCase)).Value;
    }

    private static APIGatewayProxyResponse CreateResponse(HttpStatusCode statusCode, object body)
        => new()
        {
            StatusCode = (int)statusCode,
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            Body = JsonSerializer.Serialize(body, JsonOptions)
        };
}