using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Npgsql;
using Paymentcsharp441Lambda.Models;
using Paymentcsharp441Lambda.Services;
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace Paymentcsharp441Lambda;
public sealed class Function
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly Service _service;
    public Function()
    {
        _dataSource = BuildDataSource();
        _service = new Service(_dataSource);
    }
    public async Task<APIGatewayProxyResponse> Paymentcsharp441(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var requestId = context.AwsRequestId;
        try
        {
            if (string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) && string.Equals(request.Path, "/v1/payments", StringComparison.OrdinalIgnoreCase))
            {
                var result = await _service.CreatePaymentAsync(request.Body, requestId, CancellationToken.None);
                return new APIGatewayProxyResponse { StatusCode = 201, Body = JsonSerializer.Serialize(result), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
            }
            if (string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) && string.Equals(request.Path, "/v1/payments/verify", StringComparison.OrdinalIgnoreCase))
            {
                var result = await _service.VerifyPaymentAsync(request.Body, requestId, CancellationToken.None);
                return new APIGatewayProxyResponse { StatusCode = 200, Body = JsonSerializer.Serialize(result), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
            }
            if (string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) && string.Equals(request.Path, "/v1/payments", StringComparison.OrdinalIgnoreCase))
            {
                var result = await _service.GetPaymentsAsync(request.QueryStringParameters, requestId, CancellationToken.None);
                return new APIGatewayProxyResponse { StatusCode = 200, Body = JsonSerializer.Serialize(result), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
            }
            return new APIGatewayProxyResponse { StatusCode = 404, Body = JsonSerializer.Serialize(new ErrorResponse { Code = "NOT_FOUND", Message = "Route not found." }), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
        catch (ServiceException ex)
        {
            return new APIGatewayProxyResponse { StatusCode = ex.StatusCode, Body = JsonSerializer.Serialize(new ErrorResponse { Code = ex.Code, Message = ex.Message }), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
        }
        catch
        {
            return new APIGatewayProxyResponse { StatusCode = 500, Body = JsonSerializer.Serialize(new ErrorResponse { Code = "DB_ERROR", Message = "An internal error occurred." }), Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } };
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