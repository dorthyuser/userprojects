using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Npgsql;
using Npgsql.NameTranslation;
using DemoshauntcLambda.Models;
using DemoshauntcLambda.Services;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace DemoshauntcLambda;

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

    internal static NpgsqlDataSource BuildDataSource()
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
        var dbname = SecretsHelper.Get("dbname", "POSTGRESQLDATABASE");
        var username = SecretsHelper.Get("username", "POSTGRESQLUSERNAME");
        var password = SecretsHelper.Get("password", "POSTGRESQLPASSWORD");

        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = int.TryParse(port, out var p) ? p : 5432,
            Database = dbname,
            Username = username,
            Password = password,
            Pooling = true,
            Timeout = 15,
            CommandTimeout = 30
        };
        return csb.ConnectionString;
    }

    public async Task<APIGatewayProxyResponse> demoshauntc(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            if (!request.Headers.TryGetValue("client_id", out var clientId) || string.IsNullOrWhiteSpace(clientId) || clientId.Length < 1 || clientId.Length > 128 || !System.Text.RegularExpressions.Regex.IsMatch(clientId, @"^[\w+]+$"))
                return Error(400, "Invalid or missing header 'client_id'.");

            if (string.IsNullOrWhiteSpace(request.Body))
                return Error(400, "Request body is required.");

            if (request.Headers.TryGetValue("Content-Type", out var ct) && !ct.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                return Error(400, "Invalid Content-Type header.");
            if (!request.Headers.TryGetValue("Content-Type", out _))
                return Error(400, "Missing Content-Type header.");

            var model = JsonSerializer.Deserialize<Request>(request.Body, JsonOptions);
            if (model is null) return Error(400, "Invalid request body.");
            var validation = model.Validate();
            if (validation is not null) return Error(400, validation);

            var result = await _service.CreateAsync(model);
            return new APIGatewayProxyResponse
            {
                StatusCode = 201,
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
                Body = JsonSerializer.Serialize(result, JsonOptions)
            };
        }
        catch (JsonException ex)
        {
            Console.WriteLine(ex);
            return Error(400, "Invalid JSON payload.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return Error(500, "An unexpected error occurred.");
        }
    }

    private static APIGatewayProxyResponse Error(int statusCode, string message) => new()
    {
        StatusCode = statusCode,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = JsonSerializer.Serialize(new { error = message }, JsonOptions)
    };
}