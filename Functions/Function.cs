using System.Diagnostics;
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

    internal Function(Service service)
    {
        _service = service;
    }

    public APIGatewayProxyResponse csharpae1012(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            if (string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                var result = _service.HandleGet(request.QueryStringParameters is null ? new Dictionary<string, string>() : new Dictionary<string, string>(request.QueryStringParameters), context.AwsRequestId);
                return BuildResponse(200, result);
            }

            var body = request.Body ?? string.Empty;
            var payload = JsonSerializer.Deserialize<Request>(body, JsonOptions) ?? throw new InvalidOperationException("Invalid request body.");
            var resultPost = _service.HandlePost(payload, context.AwsRequestId);
            return BuildResponse(201, resultPost);
        }
        catch (ValidationException ex)
        {
            return BuildErrorResponse(400, ex.Code, ex.Message);
        }
        catch (DuplicateAeException ex)
        {
            return BuildErrorResponse(409, ex.Code, ex.Message, ex.ExistingAeId);
        }
        catch (Exception)
        {
            return BuildErrorResponse(500, "DB_ERROR", "An internal error occurred.");
        }
        finally
        {
            sw.Stop();
        }
    }

    private static APIGatewayProxyResponse BuildResponse(int statusCode, object payload)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            },
            Body = JsonSerializer.Serialize(payload, JsonOptions)
        };
    }

    private static APIGatewayProxyResponse BuildErrorResponse(int statusCode, string code, string message, string? aeId = null)
    {
        var error = new ErrorResponse
        {
            Status = "error",
            Code = code,
            Message = message,
            AeId = aeId
        };

        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            },
            Body = JsonSerializer.Serialize(error, JsonOptions)
        };
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
}