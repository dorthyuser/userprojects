using System.Net;
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
    private readonly Service _service;

    public Function()
    {
        var dataSource = BuildDataSource();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<Service>();
        _service = new Service(dataSource, logger);
    }

    public async Task<APIGatewayProxyResponse> Csharpae1140(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var requestId = context.AwsRequestId;
        try
        {
            if (request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase) && request.Path.Equals("/v1/adverse-events", StringComparison.OrdinalIgnoreCase))
            {
                var result = await _service.SubmitAdverseEventAsync(request.Body, requestId, CancellationToken.None);
                return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.Created, Body = System.Text.Json.JsonSerializer.Serialize(result), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
            }

            if (request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase) && request.Path.Equals("/v1/adverse-events/notifications", StringComparison.OrdinalIgnoreCase))
            {
                var result = await _service.GetNotificationsAsync(request.QueryStringParameters, requestId, CancellationToken.None);
                return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.OK, Body = System.Text.Json.JsonSerializer.Serialize(result), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
            }

            return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.NotFound, Body = System.Text.Json.JsonSerializer.Serialize(new ErrorResponse { Status = "failure", Code = "NOT_FOUND", Message = "Route not found." }), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
        catch (ValidationException ex)
        {
            return new APIGatewayProxyResponse { StatusCode = ex.StatusCode, Body = System.Text.Json.JsonSerializer.Serialize(new ErrorResponse { Status = "failure", Code = ex.Code, Message = ex.Message }), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
        catch (DuplicateAeException ex)
        {
            return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.Conflict, Body = System.Text.Json.JsonSerializer.Serialize(new DuplicateAeResponse { Status = "failure", Code = "DUPLICATE_AE", AeId = ex.ExistingAeId, Message = "Identical adverse event already exists." }), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
        catch
        {
            return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.InternalServerError, Body = System.Text.Json.JsonSerializer.Serialize(new ErrorResponse { Status = "failure", Code = "DB_ERROR", Message = "Database operation failed." }), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
    }

    private static NpgsqlDataSource BuildDataSource()
    {
        var host = SecretsHelper.Get("host", "host");
        var port = SecretsHelper.Get("port", "port");
        var dbname = SecretsHelper.Get("dbname", "dbname");
        var username = SecretsHelper.Get("username", "POSTGRESQLUSERNAME");
        var password = SecretsHelper.Get("password", "password");
        var builder = new NpgsqlDataSourceBuilder($"Host={host};Port={port};Database={dbname};Username={username};Password={password}");
        return builder.Build();
    }
}