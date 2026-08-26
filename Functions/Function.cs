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
            if (request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase) && request.Path.Equals("/v1/payments", StringComparison.OrdinalIgnoreCase))
            {
                var result = await _service.InitiatePaymentAsync(request.Body, requestId, CancellationToken.None);
                return ResponseFactory.Success(201, result);
            }

            if (request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase) && request.Path.Equals("/v1/payments/verify", StringComparison.OrdinalIgnoreCase))
            {
                var result = await _service.VerifyPaymentAsync(request.Body, requestId, CancellationToken.None);
                return ResponseFactory.Success(200, result);
            }

            if (request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase) && request.Path.Equals("/v1/payments", StringComparison.OrdinalIgnoreCase))
            {
                var result = await _service.GetPaymentsAsync(request.QueryStringParameters, requestId, CancellationToken.None);
                return ResponseFactory.Success(200, result);
            }

            return ResponseFactory.Error(404, "NOT_FOUND", "Route not found.");
        }
        catch (PaymentServiceException ex)
        {
            return ResponseFactory.Error(ex.StatusCode, ex.Code, ex.Message, ex.Extra);
        }
        catch
        {
            return ResponseFactory.Error(500, "DB_ERROR", "An internal error occurred.");
        }
    }

    private static NpgsqlDataSource BuildDataSource()
    {
        var host = SecretsHelper.Get("host", "host");
        var port = SecretsHelper.Get("port", "port");
        var dbname = SecretsHelper.Get("dbname", "dbname");
        var username = SecretsHelper.Get("username", "username");
        var password = SecretsHelper.Get("password", "password");
        return BuildDataSource(host, port, dbname, username, password);
    }

    private static NpgsqlDataSource BuildDataSource(string host, string port, string dbname, string username, string password)
    {
        var builder = new NpgsqlDataSourceBuilder($"Host={host};Port={port};Database={dbname};Username={username};Password={password}");
        return builder.Build();
    }
}
