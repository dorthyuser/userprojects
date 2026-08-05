using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Net;
using System.Text.Json;
using Csharpae1140Lambda.Models;
using Csharpae1140Lambda.Services;
using MSLogLevel = Microsoft.Extensions.Logging.LogLevel;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Csharpae1140Lambda;

public sealed class Function
{
    private static readonly NpgsqlDataSource DataSource = BuildDataSource();
    private readonly Service _service;
    private readonly ILogger<Function> _logger;

    public Function()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<Function>();
        _service = new Service(DataSource, loggerFactory.CreateLogger<Service>());
    }

    public async Task<APIGatewayProxyResponse> Csharpae1140(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var requestId = context.AwsRequestId;
        var method = request.HttpMethod?.ToUpperInvariant() ?? string.Empty;
        var path = request.Path ?? string.Empty;
        _logger.LogInformation("{Method} {Path} {RequestId}", method, path, requestId);

        try
        {
            if (method == "POST" && path == "/v1/adverse-events")
            {
                var result = await _service.SubmitAdverseEventAsync(request.Body, requestId, CancellationToken.None);
                return JsonResponse((int)HttpStatusCode.Created, result);
            }

            if (method == "GET" && path == "/v1/adverse-events/notifications")
            {
                var result = await _service.GetNotificationsAsync(request.QueryStringParameters, requestId, CancellationToken.None);
                return JsonResponse((int)HttpStatusCode.OK, result);
            }

            return JsonResponse((int)HttpStatusCode.NotFound, new ErrorResponse { Status = "failure", Code = "NOT_FOUND", Message = "Route not found." });
        }
        catch (ValidationException ex)
        {
            return JsonResponse((int)HttpStatusCode.BadRequest, new ErrorResponse { Status = "failure", Code = ex.Code, Message = ex.Message });
        }
        catch (DuplicateAeException ex)
        {
            return JsonResponse((int)HttpStatusCode.Conflict, new DuplicateAeResponse { Status = "failure", Code = "DUPLICATE_AE", AeId = ex.AeId, Message = "Identical adverse event already exists." });
        }
        catch (Exception)
        {
            return JsonResponse((int)HttpStatusCode.InternalServerError, new ErrorResponse { Status = "failure", Code = "DB_ERROR", Message = "Database operation failed." });
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
        builder.MapEnum<OutcomeEnum>();
        builder.MapEnum<ActionTakenEnum>();
        builder.MapEnum<PriorityEnum>();
        return builder.Build();
    }

    private static APIGatewayProxyResponse JsonResponse(int statusCode, object body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "application/json" },
            Body = JsonSerializer.Serialize(body)
        };
    }
}