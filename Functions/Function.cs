using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Npgsql;
using Npgsql.NameTranslation;
using Csharpae1012Lambda.Models;
using Csharpae1012Lambda.Services;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Csharpae1012Lambda;

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

    private static NpgsqlDataSource BuildDataSource()
    {
        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = SecretsHelper.Get("host", "POSTGRESQLHOST"),
            Port = int.TryParse(SecretsHelper.Get("port", "POSTGRESQLPORT"), out var port) ? port : 5432,
            Database = SecretsHelper.Get("dbname", "POSTGRESQLDATABASE"),
            Username = SecretsHelper.Get("username", "POSTGRESQLUSERNAME"),
            Password = SecretsHelper.Get("password", "POSTGRESQLPASSWORD"),
            SslMode = Enum.TryParse<Npgsql.SslMode>(Environment.GetEnvironmentVariable("DB_SSL_MODE") ?? "Require", true, out var sslMode) ? sslMode : Npgsql.SslMode.Require
        };

        var builder = new NpgsqlDataSourceBuilder(csb.ConnectionString);
        builder.MapEnum<Outcome>("outcome", new NpgsqlNullNameTranslator());
        builder.MapEnum<ActionTaken>("action_taken", new NpgsqlNullNameTranslator());
        builder.MapEnum<Priority>("priority", new NpgsqlNullNameTranslator());
        return builder.Build();
    }

    public APIGatewayProxyResponse csharpae1012(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            if (string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                var getResult = _service.GetNotifications(request.QueryStringParameters is null ? new Dictionary<string, string>() : new Dictionary<string, string>(request.QueryStringParameters), context);
                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                    Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
                    Body = JsonSerializer.Serialize(getResult, JsonOptions)
                };
            }

            var payload = JsonSerializer.Deserialize<Request>(request.Body ?? "{}", JsonOptions) ?? new Request();
            var validation = Validator.ValidateAndCoerce(payload);
            if (!validation.IsValid)
            {
                return ErrorResponse(400, validation.ErrorCode!, validation.Message!);
            }

            var postResult = _service.SubmitAdverseEvent(validation.Payload!, context);
            return new APIGatewayProxyResponse
            {
                StatusCode = postResult.StatusCode,
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
                Body = JsonSerializer.Serialize(postResult.Body, JsonOptions)
            };
        }
        catch (Exception)
        {
            return ErrorResponse(500, "DB_ERROR", "An unexpected error occurred.");
        }
    }

    private static APIGatewayProxyResponse ErrorResponse(int statusCode, string code, string message)
    {
        var body = new Response
        {
            Status = "error",
            Code = code,
            Message = message
        };

        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            Body = JsonSerializer.Serialize(body, JsonOptions)
        };
    }
}

public static class Validator
{
    public static ValidationResult ValidateAndCoerce(Request request)
    {
        var required = new[] { request.TrialId, request.SiteId, request.PatientId, request.ClinicianId, request.EventDate, request.AeTermCode, request.AeTermName, request.ReportedBy, request.Narrative };
        if (required.Any(string.IsNullOrWhiteSpace))
        {
            return ValidationResult.Fail("MISSING_REQUIRED_FIELD", "One or more required fields are missing.");
        }

        if (request.CtcaeGrade < 1 || request.CtcaeGrade > 5)
        {
            return ValidationResult.Fail("INVALID_CTCAE_GRADE", "Invalid value for field 'ctcaeGrade'. Accepted values: 1, 2, 3, 4, 5");
        }

        if (!Enum.IsDefined(typeof(Outcome), request.Outcome))
        {
            return ValidationResult.Fail("INVALID_OUTCOME", "Invalid value for field 'outcome'. Accepted values: ONGOING, RESOLVED, FATAL, UNKNOWN");
        }

        if (!Enum.IsDefined(typeof(ActionTaken), request.ActionTaken))
        {
            return ValidationResult.Fail("INVALID_ACTION_TAKEN", "Invalid value for field 'actionTaken'. Accepted values: NONE, DOSE_REDUCED, DRUG_WITHDRAWN, HOSPITALISED");
        }

        if (request.Narrative.Length > 2000)
        {
            return ValidationResult.Fail("NARRATIVE_TOO_LONG", "narrative exceeds 2000 characters");
        }

        if (!DateTimeOffset.TryParse(request.EventDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var eventDate))
        {
            return ValidationResult.Fail("INVALID_QUERY_PARAM", "Invalid eventDate format.");
        }

        request.EventDate = eventDate.UtcDateTime.ToString("O");
        request.Serious = request.CtcaeGrade >= 3 || request.Serious;
        if (request.CtcaeGrade == 5)
        {
            request.Outcome = Outcome.FATAL;
        }

        return ValidationResult.Success(request);
    }
}

public sealed class ValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorCode { get; init; }
    public string? Message { get; init; }
    public Request? Payload { get; init; }

    public static ValidationResult Success(Request payload) => new() { IsValid = true, Payload = payload };
    public static ValidationResult Fail(string code, string message) => new() { IsValid = false, ErrorCode = code, Message = message };
}