using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Npgsql;
using Npgsql.NameTranslation;
using LambdatestingtravelcardLambda.Models;
using LambdatestingtravelcardLambda.Services;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace LambdatestingtravelcardLambda;

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
            Password = SecretsHelper.Get("password", "POSTGRESQLPASSWORD"),
            Pooling = true,
            Multiplexing = false
        };

        var builder = new NpgsqlDataSourceBuilder(csb.ConnectionString);
        builder.MapEnum<TravelcardType>("travelcard_type_enum", new NpgsqlNullNameTranslator());
        builder.MapEnum<CardholderType>("cardholder_type_enum", new NpgsqlNullNameTranslator());
        return builder.Build();
    }

    public async Task<APIGatewayProxyResponse> lambdatestingtravelcard(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            if (!request.Headers.TryGetValue("client_id", out var clientId) || string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
            {
                return ErrorResponse(HttpStatusCode.BadRequest, "Missing or invalid header 'client_id'.");
            }

            if (request.Body is null || string.IsNullOrWhiteSpace(request.Body))
            {
                return ErrorResponse(HttpStatusCode.BadRequest, "Request body is required.");
            }

            Request? input;
            try
            {
                input = JsonSerializer.Deserialize<Request>(request.Body, JsonOptions);
            }
            catch (JsonException ex)
            {
                return ErrorResponse(HttpStatusCode.BadRequest, $"Invalid JSON payload: {ex.Message}");
            }

            if (input is null)
            {
                return ErrorResponse(HttpStatusCode.BadRequest, "Request body is required.");
            }

            var validationError = Validate(input);
            if (validationError is not null)
            {
                return ErrorResponse(HttpStatusCode.BadRequest, validationError);
            }

            var result = await _service.CreateAsync(input);
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
        catch (Exception ex)
        {
            Console.WriteLine($"Unhandled error: {ex}");
            return ErrorResponse(HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static string? Validate(Request input)
    {
        var now = DateTimeOffset.UtcNow;

        if (input.TravelcardValidFrom > now.AddMonths(1))
            return "travelcardValidFrom must be no later than one calendar month from today.";

        if (input.TravelcardRequestedDate >= now)
            return "travelcardRequestedDate must be in the past.";

        if (input.TravelcardValidFrom > input.TravelcardValidTo)
            return "travelcardValidFrom cannot be later than travelcardValidTo.";

        if (input.TravelcardValidTo <= now)
            return "travelcardValidTo must be in the future.";

        if (input.TravelcardType == TravelcardType.SixteenToSeventeen)
        {
            if (input.TravelcardUsableTo is null)
                return "travelcardUsableTo is required for SixteenToSeventeen travelcards.";
            if (input.TravelcardUsableTo <= now)
                return "travelcardUsableTo must be in the future.";
        }

        if (input.TravelcardUsableTo.HasValue && input.TravelcardUsableTo.Value <= now)
            return "travelcardUsableTo must be in the future.";

        if (input.Cardholders is null || input.Cardholders.Count < 1 || input.Cardholders.Count > 2)
            return "cardholders must contain exactly one primary cardholder and optionally one secondary cardholder.";

        var primaryCount = input.Cardholders.Count(c => c.CardholderType == CardholderType.Primary);
        if (primaryCount != 1)
            return "Exactly one Primary cardholder is required.";

        var secondaryCount = input.Cardholders.Count(c => c.CardholderType == CardholderType.Secondary);
        if (secondaryCount > 1)
            return "Only one Secondary cardholder is allowed.";

        if ((input.TravelcardType == TravelcardType.SixteenToSeventeen || input.TravelcardType == TravelcardType.Veterans) && secondaryCount > 0)
            return "Secondary cardholder is not allowed for SixteenToSeventeen and Veterans travelcard types.";

        return null;
    }

    private static APIGatewayProxyResponse ErrorResponse(HttpStatusCode statusCode, string message)
    {
        var payload = new { error = new { code = (int)statusCode, message } };
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            },
            Body = JsonSerializer.Serialize(payload, JsonOptions)
        };
    }
}