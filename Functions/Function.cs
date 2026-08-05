using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.Extensions.Logging;
using Npgsql;
using Csharpae1140Lambda.Models;
using Csharpae1140Lambda.Services;
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace Csharpae1140Lambda;
public sealed class Function
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly NpgsqlDataSource _dataSource;
    private readonly Service _service;
    private readonly ILogger<Function> _logger;
    public Function()
    {
        _dataSource = BuildDataSource();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<Function>();
        _service = new Service(_dataSource, loggerFactory.CreateLogger<Service>());
    }
    public async Task<APIGatewayProxyResponse> Csharpae1140(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var requestId = context.AwsRequestId;
        _logger.LogInformation("HTTP {Method} {Path} {RequestId}", request.HttpMethod, request.Path, requestId);
        try
        {
            if (string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) && string.Equals(request.Path, "/v1/adverse-events", StringComparison.OrdinalIgnoreCase))
            {
                var result = await _service.SubmitAdverseEventAsync(request.Body, requestId, CancellationToken.None);
                return new APIGatewayProxyResponse { StatusCode = 201, Body = JsonSerializer.Serialize(result, JsonOptions), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
            }
            if (string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) && string.Equals(request.Path, "/v1/adverse-events/notifications", StringComparison.OrdinalIgnoreCase))
            {
                var result = await _service.GetNotificationsAsync(request.QueryStringParameters, requestId, CancellationToken.None);
                return new APIGatewayProxyResponse { StatusCode = 200, Body = JsonSerializer.Serialize(result, JsonOptions), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
            }
            return new APIGatewayProxyResponse { StatusCode = 404, Body = JsonSerializer.Serialize(new ErrorResponse { Status = "error", Code = "NOT_FOUND", Message = "Route not found." }, JsonOptions), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
        catch (AdverseEventValidationException ex)
        {
            return new APIGatewayProxyResponse { StatusCode = ex.StatusCode, Body = JsonSerializer.Serialize(new ErrorResponse { Status = "error", Code = ex.Code, Message = ex.Message }, JsonOptions), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
        catch (AdverseEventConflictException ex)
        {
            return new APIGatewayProxyResponse { StatusCode = 409, Body = JsonSerializer.Serialize(new DuplicateAeResponse { Status = "error", Code = "DUPLICATE_AE", AeId = ex.AeId }, JsonOptions), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
        catch
        {
            return new APIGatewayProxyResponse { StatusCode = 500, Body = JsonSerializer.Serialize(new ErrorResponse { Status = "error", Code = "DB_ERROR", Message = "Database operation failed." }, JsonOptions), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
    }
    private static NpgsqlDataSource BuildDataSource()
    {
        var host = SecretsHelper.Get("host", "host");
        var port = SecretsHelper.Get("port", "port");
        var dbname = SecretsHelper.Get("dbname", "dbname");
        var username = SecretsHelper.Get("username", "POSTGRESQLUSERNAME");
        var password = SecretsHelper.Get("password", "password");
        return BuildDataSource(host, port, dbname, username, password);
    }
    private static NpgsqlDataSource BuildDataSource(string host, string port, string dbname, string username, string password)
    {
        var builder = new NpgsqlDataSourceBuilder($"Host={host};Port={port};Database={dbname};Username={username};Password={password}");
        return builder.Build();
    }
}