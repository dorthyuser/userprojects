using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.Extensions.Logging;
using Npgsql;
using Paymentcsharp441Lambda.Models;
using Paymentcsharp441Lambda.Services;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Paymentcsharp441Lambda;

public sealed class Function
{
    private readonly Service _service;
    private readonly ILogger<Function> _logger;

    public Function()
    {
        var dataSource = BuildDataSource(
            SecretsHelper.Get("host", "host"),
            SecretsHelper.Get("port", "port"),
            SecretsHelper.Get("dbname", "dbname"),
            SecretsHelper.Get("username", "POSTGRESQLUSERNAME"),
            SecretsHelper.Get("password", "password"));
        _service = new Service(dataSource);
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<Function>();
    }

    public async Task<APIGatewayProxyResponse> Paymentcsharp441(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var requestId = context.AwsRequestId;
        _logger.LogInformation("{Method} {Path} {RequestId}", request.HttpMethod, request.Path, requestId);
        try
        {
            if (string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) && string.Equals(request.Path, "/v1/payments", StringComparison.OrdinalIgnoreCase))
            {
                var result = await _service.InitiatePaymentAsync(request.Body, requestId, CancellationToken.None);
                return JsonResponse(201, result);
            }
            if (string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) && string.Equals(request.Path, "/v1/payments/verify", StringComparison.OrdinalIgnoreCase))
            {
                var result = await _service.VerifyPaymentAsync(request.Body, requestId, CancellationToken.None);
                return JsonResponse(200, result);
            }
            if (string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) && string.Equals(request.Path, "/v1/payments", StringComparison.OrdinalIgnoreCase))
            {
                var result = await _service.GetPaymentsAsync(request.QueryStringParameters, requestId, CancellationToken.None);
                return JsonResponse(200, result);
            }
            return JsonResponse(404, new ErrorResponse { Status = "error", Code = "NOT_FOUND", Message = "Route not found." });
        }
        catch (PaymentValidationException ex)
        {
            return JsonResponse(ex.StatusCode, new ErrorResponse { Status = "error", Code = ex.Code, Message = ex.Message });
        }
        catch (PaymentConflictException ex)
        {
            return JsonResponse(ex.StatusCode, new ErrorResponse { Status = "error", Code = ex.Code, Message = ex.Message });
        }
        catch (PaymentGatewayException)
        {
            return JsonResponse(500, new ErrorResponse { Status = "error", Code = "GATEWAY_ERROR", Message = "Gateway order creation failed." });
        }
        catch (Exception)
        {
            return JsonResponse(500, new ErrorResponse { Status = "error", Code = "DB_ERROR", Message = "Database operation failed." });
        }
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

    private static NpgsqlDataSource BuildDataSource(string host, string port, string dbname, string username, string password)
    {
        var builder = new NpgsqlDataSourceBuilder($"Host={host};Port={port};Database={dbname};Username={username};Password={password}");
        return builder.Build();
    }
}