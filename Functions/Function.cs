using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Npgsql;
using Csharpae1039Lambda.Models;
using Csharpae1039Lambda.Services;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Csharpae1039Lambda;

public sealed class Function
{
    private readonly Service _service;

    public Function()
    {
        var dataSource = BuildDataSource(
            SecretsHelper.Get("host", "host"),
            SecretsHelper.Get("port", "port"),
            SecretsHelper.Get("dbname", "dbname"),
            SecretsHelper.Get("username", "username"),
            SecretsHelper.Get("password", "password"));
        _service = new Service(dataSource);
    }

    public async Task<APIGatewayProxyResponse> Csharpae1039(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var path = request.Path ?? string.Empty;
        var method = request.HttpMethod ?? string.Empty;
        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) && path.Equals("/v1/adverse-events", StringComparison.OrdinalIgnoreCase))
        {
            var result = await _service.SubmitAdverseEventAsync(request.Body, CancellationToken.None);
            return BuildResponse(result.StatusCode, result.Body);
        }
        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) && path.Equals("/v1/adverse-events/notifications", StringComparison.OrdinalIgnoreCase))
        {
            var result = await _service.GetNotificationsAsync(request.QueryStringParameters, CancellationToken.None);
            return BuildResponse(result.StatusCode, result.Body);
        }
        return BuildResponse(404, JsonSerializer.Serialize(new ErrorResponse { Code = "NOT_FOUND", Message = "Route not found." }));
    }

    private static APIGatewayProxyResponse BuildResponse(int statusCode, string body) => new()
    {
        StatusCode = statusCode,
        Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "application/json" },
        Body = body
    };

    private static NpgsqlDataSource BuildDataSource(string host, string port, string dbname, string username, string password)
    {
        var builder = new NpgsqlDataSourceBuilder($"Host={host};Port={port};Database={dbname};Username={username};Password={password}");
        builder.MapEnum<OutcomeEnum>();
        builder.MapEnum<ActionTakenEnum>();
        builder.MapEnum<PriorityEnum>();
        builder.MapEnum<LevelEnum>();
        return builder.Build();
    }
}