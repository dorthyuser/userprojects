using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Npgsql;
using Npgsql.NameTranslation;
using Aelambda1024Lambda.Models;
using Aelambda1024Lambda.Services;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Aelambda1024Lambda;

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

    public async Task<APIGatewayProxyResponse> aelambda1024(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            var path = request.Path ?? string.Empty;
            var method = request.HttpMethod ?? string.Empty;

            if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) && path.Equals("/v1/adverse-events", StringComparison.OrdinalIgnoreCase))
            {
                var body = string.IsNullOrWhiteSpace(request.Body)
                    ? null
                    : JsonSerializer.Deserialize<AdverseEventRequest>(request.Body, JsonOptions);

                var result = await _service.SubmitAdverseEventAsync(body, context);
                return BuildResponse(HttpStatusCode.Created, result);
            }

            if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) && path.Equals("/v1/adverse-events/notifications", StringComparison.OrdinalIgnoreCase))
            {
                var result = await _service.GetNotificationsAsync(request.QueryStringParameters is null ? null : new Dictionary<string, string>(request.QueryStringParameters), context);
                return BuildResponse(HttpStatusCode.OK, result);
            }

            return BuildResponse(HttpStatusCode.NotFound, new ErrorResponse("NOT_FOUND", "Route not found"));
        }
        catch (ApiException ex)
        {
            return BuildResponse((HttpStatusCode)ex.StatusCode, new ErrorResponse(ex.Code, ex.Message, ex.ExistingAeId));
        }
        catch
        {
            return BuildResponse(HttpStatusCode.InternalServerError, new ErrorResponse("DB_ERROR", "An unexpected error occurred"));
        }
    }

    private static APIGatewayProxyResponse BuildResponse(HttpStatusCode statusCode, object payload)
    {
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

    private static NpgsqlDataSource BuildDataSource()
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = SecretsHelper.Get("host", "POSTGRESQLHOST"),
            Port = int.TryParse(SecretsHelper.Get("port", "POSTGRESQLPORT"), out var port) ? port : 5432,
            Database = SecretsHelper.Get("dbname", "POSTGRESQLDATABASE"),
            Username = SecretsHelper.Get("username", "POSTGRESQLUSERNAME"),
            Password = SecretsHelper.Get("password", "POSTGRESQLPASSWORD"),
            Pooling = true,
            IncludeErrorDetail = false,
            Timeout = 15,
            CommandTimeout = 30
        };

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.ConnectionString);
        dataSourceBuilder.MapEnum<AeOutcome>("outcome", new NpgsqlNullNameTranslator());
        dataSourceBuilder.MapEnum<AeActionTaken>("action_taken", new NpgsqlNullNameTranslator());
        dataSourceBuilder.MapEnum<NotificationPriority>("priority", new NpgsqlNullNameTranslator());
        return dataSourceBuilder.Build();
    }
}