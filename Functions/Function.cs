using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text.Json;
using System.Text.Json.Serialization;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Csharpae1039Lambda;

public sealed class Function
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(null, allowIntegerValues: false) }
    };

    private readonly Service _service;
    private readonly ILogger<Function> _logger;

    public Function()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<Function>();
        _service = new Service(BuildDataSource(), loggerFactory.CreateLogger<Service>());
    }

    public async Task<APIGatewayProxyResponse> Csharpae1039(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var requestId = context.AwsRequestId;
        var method = request.HttpMethod ?? string.Empty;
        var path = request.Path ?? string.Empty;
        _logger.LogInformation("HTTP {Method} {Path} request_id={RequestId}", method, path, requestId);

        try
        {
            if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase) && string.Equals(path, "/v1/adverse-events", StringComparison.OrdinalIgnoreCase))
            {
                return await _service.HandlePostAsync(request.Body, requestId, CancellationToken.None).ConfigureAwait(false);
            }

            if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) && string.Equals(path, "/v1/adverse-events/notifications", StringComparison.OrdinalIgnoreCase))
            {
                return await _service.HandleGetAsync(request.QueryStringParameters is null ? new Dictionary<string, string>() : new Dictionary<string, string>(request.QueryStringParameters), requestId, CancellationToken.None).ConfigureAwait(false);
            }

            return BuildResponse(404, new { status = "failure", code = "NOT_FOUND", message = "Route not found." });
        }
        catch
        {
            return BuildResponse(500, new { status = "failure", code = "DB_ERROR", message = "Internal server error." });
        }
    }

    private static NpgsqlDataSource BuildDataSource()
    {
        var builder = new NpgsqlDataSourceBuilder(BuildConnectionString());
        builder.MapEnum<AeOutcome>();
        builder.MapEnum<AeActionTaken>();
        builder.MapEnum<NotificationPriority>();
        builder.MapEnum<AuditAction>();
        return builder.Build();
    }

    private static string BuildConnectionString()
    {
        var host = SecretsHelper.Get("host", "host");
        var port = SecretsHelper.Get("port", "port");
        var dbname = SecretsHelper.Get("dbname", "dbname");
        var username = SecretsHelper.Get("username", "username");
        var password = SecretsHelper.Get("password", "password");
        return $"Host={host};Port={port};Database={dbname};Username={username};Password={password};Pooling=true;Maximum Pool Size=20;Timeout=15;Command Timeout=30";
    }

    private static APIGatewayProxyResponse BuildResponse(int statusCode, object body) => new()
    {
        StatusCode = statusCode,
        Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Content-Type"] = "application/json"
        },
        Body = JsonSerializer.Serialize(body, JsonOptions)
    };
}