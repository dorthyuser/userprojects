using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Microsoft.Extensions.Logging;
using Npgsql;
using Buyandsellgold1013Lambda.Models;
using Buyandsellgold1013Lambda.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Buyandsellgold1013Lambda;

public sealed class Function
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly Service _service;
    private readonly ILogger<Function> _logger;

    public Function()
    {
        var host = SecretsHelper.Get("host", "host");
        var port = SecretsHelper.Get("port", "port");
        var dbname = SecretsHelper.Get("dbname", "dbname");
        var username = SecretsHelper.Get("username", "POSTGRESQLUSERNAME");
        var password = SecretsHelper.Get("password", "password");
        _dataSource = BuildDataSource(host, port, dbname, username, password);
        _service = new Service(_dataSource);
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<Function>();
    }

    public async Task<APIGatewayProxyResponse> Buyandsellgold1013(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var method = request.HttpMethod?.ToUpperInvariant() ?? string.Empty;
        var path = request.Path ?? string.Empty;
        var identifier = request.PathParameters != null && request.PathParameters.Count > 0 ? string.Join(",", request.PathParameters.Values) : string.Empty;
        _logger.LogInformation("{Method} {Path} {Identifier}", method, path, identifier);

        try
        {
            return method switch
            {
                "GET" when path.Equals("/api/v1/products", StringComparison.OrdinalIgnoreCase) => await HandleGetProductsAsync(request),
                "GET" when path.Equals("/api/v1/products/{productId}", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/api/v1/products/", StringComparison.OrdinalIgnoreCase) => await HandleGetProductAsync(request),
                "POST" when path.Equals("/api/v1/buy-orders", StringComparison.OrdinalIgnoreCase) => await HandleCreateBuyOrderAsync(request),
                "POST" when path.Equals("/api/v1/sell-requests", StringComparison.OrdinalIgnoreCase) => await HandleCreateSellRequestAsync(request),
                "GET" when path.Equals("/api/v1/buy-orders/{orderId}", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/api/v1/buy-orders/", StringComparison.OrdinalIgnoreCase) => await HandleGetBuyOrderAsync(request),
                "GET" when path.Equals("/api/v1/sell-requests/{sellRequestId}", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/api/v1/sell-requests/", StringComparison.OrdinalIgnoreCase) => await HandleGetSellRequestAsync(request),
                _ => JsonResponse(HttpStatusCode.NotFound, new ErrorResponse { ErrorCode = "NOT_FOUND", ErrorMessage = "Route not found." })
            };
        }
        catch
        {
            return JsonResponse(HttpStatusCode.InternalServerError, new ErrorResponse { ErrorCode = "INTERNAL_SERVER_ERROR", ErrorMessage = "An unexpected error occurred." });
        }
    }

    private async Task<APIGatewayProxyResponse> HandleGetProductsAsync(APIGatewayProxyRequest request)
    {
        var result = await _service.GetProductsAsync(request.QueryStringParameters, CancellationToken.None);
        return JsonResponse(HttpStatusCode.OK, result);
    }

    private async Task<APIGatewayProxyResponse> HandleGetProductAsync(APIGatewayProxyRequest request)
    {
        var productId = request.PathParameters != null && request.PathParameters.TryGetValue("productId", out var value) ? value : string.Empty;
        var result = await _service.GetProductAsync(productId, CancellationToken.None);
        return JsonResponse(HttpStatusCode.OK, result);
    }

    private async Task<APIGatewayProxyResponse> HandleCreateBuyOrderAsync(APIGatewayProxyRequest request)
    {
        var model = System.Text.Json.JsonSerializer.Deserialize<CreateBuyOrderRequest>(request.Body ?? string.Empty, JsonOptions()) ?? throw new InvalidOperationException();
        var result = await _service.CreateBuyOrderAsync(model, CancellationToken.None);
        return JsonResponse(HttpStatusCode.Created, result);
    }

    private async Task<APIGatewayProxyResponse> HandleCreateSellRequestAsync(APIGatewayProxyRequest request)
    {
        var model = System.Text.Json.JsonSerializer.Deserialize<CreateSellRequestRequest>(request.Body ?? string.Empty, JsonOptions()) ?? throw new InvalidOperationException();
        var result = await _service.CreateSellRequestAsync(model, CancellationToken.None);
        return JsonResponse(HttpStatusCode.Created, result);
    }

    private async Task<APIGatewayProxyResponse> HandleGetBuyOrderAsync(APIGatewayProxyRequest request)
    {
        var orderId = request.PathParameters != null && request.PathParameters.TryGetValue("orderId", out var value) ? value : string.Empty;
        var result = await _service.GetBuyOrderAsync(orderId, CancellationToken.None);
        return JsonResponse(HttpStatusCode.OK, result);
    }

    private async Task<APIGatewayProxyResponse> HandleGetSellRequestAsync(APIGatewayProxyRequest request)
    {
        var sellRequestId = request.PathParameters != null && request.PathParameters.TryGetValue("sellRequestId", out var value) ? value : string.Empty;
        var result = await _service.GetSellRequestAsync(sellRequestId, CancellationToken.None);
        return JsonResponse(HttpStatusCode.OK, result);
    }

    private static APIGatewayProxyResponse JsonResponse<T>(HttpStatusCode statusCode, T body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Content-Type"] = "application/json"
            },
            Body = System.Text.Json.JsonSerializer.Serialize(body, JsonOptions())
        };
    }

    private static System.Text.Json.JsonSerializerOptions JsonOptions() => new(System.Text.Json.JsonSerializerDefaults.Web);

    private static NpgsqlDataSource BuildDataSource(string host, string port, string dbname, string username, string password)
    {
        var builder = new NpgsqlDataSourceBuilder($"Host={host};Port={port};Database={dbname};Username={username};Password={password}");
        return builder.Build();
    }
}