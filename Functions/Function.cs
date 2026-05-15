using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Npgsql;
using Npgsql.NameTranslation;
using Travelcarddemo1251Lambda.Models;
using Travelcarddemo1251Lambda.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Travelcarddemo1251Lambda;

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

    public async Task<APIGatewayProxyResponse> travelcarddemo1251(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var clientId = GetHeader(request.Headers, "client_id");
        context.Logger.LogLine($"HTTP Method: {request.HttpMethod}, Route: {request.Path}, client_id: {clientId ?? string.Empty}");

        try
        {
            context.Logger.LogLine("Validating request...");
            var validationError = ValidateRequest(request, out var model);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                context.Logger.LogLine(validationError);
                return CreateResponse(HttpStatusCode.BadRequest, new ErrorResponse { Message = validationError });
            }
            context.Logger.LogLine("Validation passed.");

            var result = await _service.CreateTravelcardAsync(model!, context);
            context.Logger.LogLine($"Generated ID: {result.TravelcardId}");
            return CreateResponse(HttpStatusCode.Created, result);
        }
        catch (InvalidEnumArgumentException ex)
        {
            context.Logger.LogLine($"Validation failure: {ex.Message}");
            return CreateResponse(HttpStatusCode.BadRequest, new ErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Exception in travelcarddemo1251: {ex.Message}");
            return CreateResponse(HttpStatusCode.InternalServerError, new ErrorResponse { Message = "An unexpected error occurred." });
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
            Pooling = true
        };

        var builder = new NpgsqlDataSourceBuilder(csb.ConnectionString);
        builder.MapEnum<TravelcardTypeEnum>("travelcard_type_enum", new NpgsqlNullNameTranslator());
        builder.MapEnum<CardholderTypeEnum>("cardholder_type_enum", new NpgsqlNullNameTranslator());
        return builder.Build();
    }

    private static string? GetHeader(IDictionary<string, string>? headers, string key)
    {
        if (headers == null) return null;
        foreach (var kv in headers)
        {
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase)) return kv.Value;
        }
        return null;
    }

    private static string? ValidateRequest(APIGatewayProxyRequest request, out CreateTravelcardRequest? model)
    {
        model = null;
        if (string.IsNullOrWhiteSpace(GetHeader(request.Headers, "client_id"))) return "Missing required header: client_id";
        if (!string.IsNullOrWhiteSpace(GetHeader(request.Headers, "X-Correlation-Cust-Id")) && !System.Text.RegularExpressions.Regex.IsMatch(GetHeader(request.Headers, "X-Correlation-Cust-Id")!, @"^[A-Za-z0-9_-]{1,100}$")) return "Invalid header: X-Correlation-Cust-Id";
        if (request.Body == null) return "Missing request body";
        model = JsonSerializer.Deserialize<CreateTravelcardRequest>(request.Body, JsonOptions);
        if (model == null) return "Invalid request body";
        var now = DateTimeOffset.UtcNow;
        if (!Enum.IsDefined(typeof(TravelcardTypeEnum), model.TravelcardType)) throw new InvalidEnumArgumentException($"Invalid value for field 'travelcardType'. Accepted values: Young, TwoTogether, Family, Senior, Network, TwentySixToThirty, SixteenToSeventeen, Veterans");
        if (model.TravelcardRequestedDate >= now) return "travelcardRequestedDate must be in the past";
        if (model.TravelcardValidFrom > now.AddMonths(1)) return "travelcardValidFrom must be no later than one calendar month from today";
        if (model.TravelcardValidFrom > model.TravelcardValidTo) return "travelcardValidFrom must be later than travelcardValidTo";
        if (model.TravelcardValidTo <= now) return "travelcardValidTo must be in the future";
        if (model.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen && model.TravelcardUsableTo == null) return "travelcardUsableTo is required for SixteenToSeventeen";
        if (model.TravelcardUsableTo.HasValue && model.TravelcardUsableTo.Value <= now) return "travelcardUsableTo must be in the future";
        if ((model.TravelcardType == TravelcardTypeEnum.SixteenToSeventeen || model.TravelcardType == TravelcardTypeEnum.Veterans) && model.Cardholders.Any(c => c.CardholderType == CardholderTypeEnum.Secondary)) return "Secondary cardholder is not allowed for this travelcard type";
        return null;
    }

    private static APIGatewayProxyResponse CreateResponse(HttpStatusCode code, object body) => new()
    {
        StatusCode = (int)code,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = JsonSerializer.Serialize(body, JsonOptions)
    };
}

public class InvalidEnumArgumentException : Exception
{
    public InvalidEnumArgumentException(string message) : base(message) { }
}