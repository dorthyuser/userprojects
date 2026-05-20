using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using DemotravelcardLambda.Models;
using DemotravelcardLambda.Services;
using Npgsql;
using Npgsql.NameTranslation;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace DemotravelcardLambda;

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
        var dataSource = BuildDataSource();
        _service = new Service(dataSource);
    }

    internal static NpgsqlDataSource BuildDataSource()
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = SecretsHelper.Get("host", "POSTGRESQLHOST"),
            Port = int.TryParse(SecretsHelper.Get("port", "POSTGRESQLPORT"), out var port) ? port : 5432,
            Database = SecretsHelper.Get("dbname", "POSTGRESQLDATABASE"),
            Username = SecretsHelper.Get("username", "POSTGRESQLUSERNAME"),
            Password = SecretsHelper.Get("password", "POSTGRESQLPASSWORD")
        };

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.ConnectionString);
        dataSourceBuilder.MapEnum<TravelcardType>("travelcard_type_enum", new NpgsqlNullNameTranslator());
        dataSourceBuilder.MapEnum<CardholderType>("cardholder_type_enum", new NpgsqlNullNameTranslator());
        return dataSourceBuilder.Build();
    }

    public async Task<APIGatewayProxyResponse> demotravelcard(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            if (!request.Headers.TryGetValue("client_id", out var clientId) || string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128 || !System.Text.RegularExpressions.Regex.IsMatch(clientId, @"^[\w+]+$"))
            {
                return BadRequest("Invalid or missing header 'client_id'.");
            }

            if (!request.Headers.TryGetValue("Content-Type", out var contentType) || !contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Invalid or missing header 'Content-Type'.");
            }

            var correlationId = request.Headers.TryGetValue("X-Correlation-Cust-Id", out var cid) ? cid : null;
            if (correlationId is not null && (correlationId.Length > 100 || !System.Text.RegularExpressions.Regex.IsMatch(correlationId, @"^[A-Za-z0-9_-]+$")))
            {
                return BadRequest("Invalid header 'X-Correlation-Cust-Id'.");
            }

            if (string.IsNullOrWhiteSpace(request.Body))
            {
                return BadRequest("Request body is required.");
            }

            Request? model;
            try
            {
                model = JsonSerializer.Deserialize<Request>(request.Body, JsonOptions);
            }
            catch (JsonException)
            {
                return BadRequest("Invalid JSON payload.");
            }

            if (model is null)
            {
                return BadRequest("Invalid JSON payload.");
            }

            var validationError = Validate(model);
            if (validationError is not null)
            {
                return BadRequest(validationError);
            }

            var result = await _service.CreateAsync(model);
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
                Body = JsonSerializer.Serialize(result, JsonOptions)
            };
        }
        catch (Exception)
        {
            return ErrorResponse("Internal server error.");
        }
    }

    private static string? Validate(Request model)
    {
        if (model.TravelcardType is null)
            return InvalidEnum(nameof(model.TravelcardType), Enum.GetNames(typeof(TravelcardType)));
        if (model.TravelcardValidFrom > model.TravelcardValidTo)
            return "Check valid_from date is later than valid_to date.";
        if (model.TravelcardRequestedDate >= DateTimeOffset.UtcNow)
            return "Check requested_date is in the past.";
        if (model.TravelcardValidTo <= DateTimeOffset.UtcNow)
            return "Check valid_to date is in the future.";
        if (model.TravelcardUsableTo is not null && model.TravelcardUsableTo <= DateTimeOffset.UtcNow)
            return "Check usable_to date is in the future.";
        if (model.TravelcardType == TravelcardType.SixteenToSeventeen && model.TravelcardUsableTo is null)
            return "travelcardUsableTo is required for Travelcard Type 'SixteenToSeventeen'.";
        if (model.TravelcardType != TravelcardType.SixteenToSeventeen && model.TravelcardUsableTo is not null)
            return "travelcardUsableTo is only allowed for Travelcard Type 'SixteenToSeventeen'.";
        if (model.Cardholders is null || model.Cardholders.Count is < 1 or > 2)
            return "cardholders must contain exactly one Primary and optional one Secondary cardholder.";
        if (model.Cardholders.Count(c => c.CardholderType == CardholderType.Primary) != 1)
            return "Exactly one Primary cardholder is required.";
        if (model.Cardholders.Count(c => c.CardholderType == CardholderType.Secondary) > 1)
            return "Only one Secondary cardholder is allowed.";
        if (model.Cardholders.Any(c => c.CardholderType == CardholderType.Secondary) && model.TravelcardType is not (TravelcardType.TwoTogether or TravelcardType.Family))
            return "Check secondary cardholder (optional) is allowed for Travelcard type.";
        return null;
    }

    private static string InvalidEnum(string fieldName, IEnumerable<string> values) => $"Invalid value for field '{fieldName}'. Accepted values: {string.Join(", ", values)}";

    private static APIGatewayProxyResponse BadRequest(string message) => ErrorResponse(message, HttpStatusCode.BadRequest);

    private static APIGatewayProxyResponse ErrorResponse(string message, HttpStatusCode code = HttpStatusCode.InternalServerError) => new()
    {
        StatusCode = (int)code,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = JsonSerializer.Serialize(new { message })
    };
}