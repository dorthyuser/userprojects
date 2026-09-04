using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Buyandsellgold1013Lambda.Models;
using Buyandsellgold1013Lambda.Services;
using Npgsql;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Buyandsellgold1013Lambda;

public sealed class Function
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly Service _service;

    public Function()
    {
        _dataSource = BuildDataSource(
            SecretsHelper.Get("host", "host"),
            SecretsHelper.Get("port", "port"),
            SecretsHelper.Get("dbname", "dbname"),
            SecretsHelper.Get("username", "POSTGRESQLUSERNAME"),
            SecretsHelper.Get("password", "password"));
        _service = new Service(_dataSource);
    }

    public async Task<APIGatewayProxyResponse> Buyandsellgold1013(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var method = request.HttpMethod?.Trim().ToUpperInvariant() ?? string.Empty;
        var path = request.Path ?? string.Empty;
        var requestId = context.AwsRequestId;

        try
        {
            return method switch
            {
                "GET" when path.Equals("/api/v1/products", StringComparison.OrdinalIgnoreCase) => await HandleGetProductsAsync(request, requestId),
                "GET" when path.StartsWith("/api/v1/products/", StringComparison.OrdinalIgnoreCase) => await HandleGetProductAsync(request, requestId),
                "POST" when path.Equals("/api/v1/buy-orders", StringComparison.OrdinalIgnoreCase) => await HandleCreateBuyOrderAsync(request, requestId),
                "POST" when path.Equals("/api/v1/sell-requests", StringComparison.OrdinalIgnoreCase) => await HandleCreateSellRequestAsync(request, requestId),
                "GET" when path.StartsWith("/api/v1/buy-orders/", StringComparison.OrdinalIgnoreCase) => await HandleGetBuyOrderAsync(request, requestId),
                "GET" when path.StartsWith("/api/v1/sell-requests/", StringComparison.OrdinalIgnoreCase) => await HandleGetSellRequestAsync(request, requestId),
                _ => JsonResponse(HttpStatusCode.NotFound, new ErrorResponse { ErrorCode = "NOT_FOUND", ErrorMessage = "Route not found." })
            };
        }
        catch
        {
            return JsonResponse(HttpStatusCode.InternalServerError, new ErrorResponse { ErrorCode = "INTERNAL_SERVER_ERROR", ErrorMessage = "An unexpected error occurred." });
        }
    }

    private async Task<APIGatewayProxyResponse> HandleGetProductsAsync(APIGatewayProxyRequest request, string requestId)
    {
        var result = await _service.GetProductsAsync(request.QueryStringParameters, requestId, CancellationToken.None);
        return JsonResponse(HttpStatusCode.OK, result);
    }

    private async Task<APIGatewayProxyResponse> HandleGetProductAsync(APIGatewayProxyRequest request, string requestId)
    {
        var productId = request.PathParameters != null && request.PathParameters.TryGetValue("productId", out var value) ? value : string.Empty;
        var result = await _service.GetProductAsync(productId, requestId, CancellationToken.None);
        return JsonResponse(HttpStatusCode.OK, result);
    }

    private async Task<APIGatewayProxyResponse> HandleCreateBuyOrderAsync(APIGatewayProxyRequest request, string requestId)
    {
        var model = JsonSerializer.Deserialize<CreateBuyOrderRequest>(request.Body ?? string.Empty) ?? throw new InvalidOperationException("Invalid request body.");
        var result = await _service.CreateBuyOrderAsync(model, requestId, CancellationToken.None);
        return JsonResponse(HttpStatusCode.Created, result);
    }

    private async Task<APIGatewayProxyResponse> HandleCreateSellRequestAsync(APIGatewayProxyRequest request, string requestId)
    {
        var model = JsonSerializer.Deserialize<CreateSellRequest>(request.Body ?? string.Empty) ?? throw new InvalidOperationException("Invalid request body.");
        var result = await _service.CreateSellRequestAsync(model, requestId, CancellationToken.None);
        return JsonResponse(HttpStatusCode.Created, result);
    }

    private async Task<APIGatewayProxyResponse> HandleGetBuyOrderAsync(APIGatewayProxyRequest request, string requestId)
    {
        var orderId = request.PathParameters != null && request.PathParameters.TryGetValue("orderId", out var value) ? value : string.Empty;
        var result = await _service.GetBuyOrderAsync(orderId, requestId, CancellationToken.None);
        return JsonResponse(HttpStatusCode.OK, result);
    }

    private async Task<APIGatewayProxyResponse> HandleGetSellRequestAsync(APIGatewayProxyRequest request, string requestId)
    {
        var sellRequestId = request.PathParameters != null && request.PathParameters.TryGetValue("sellRequestId", out var value) ? value : string.Empty;
        var result = await _service.GetSellRequestAsync(sellRequestId, requestId, CancellationToken.None);
        return JsonResponse(HttpStatusCode.OK, result);
    }

    private static APIGatewayProxyResponse JsonResponse(HttpStatusCode statusCode, object body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Content-Type"] = "application/json"
            },
            Body = JsonSerializer.Serialize(body)
        };
    }

    private static NpgsqlDataSource BuildDataSource(string host, string port, string dbname, string username, string password)
    {
        var builder = new NpgsqlDataSourceBuilder($"Host={host};Port={port};Database={dbname};Username={username};Password={password}");
        return builder.Build();
    }
}