using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Httpcsharplambda.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace HttpcsharplambdaLambda;

public class Function
{
    private static readonly ServiceProvider ServiceProvider;

    static Function()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddHttpClient("travelcard-api", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddHttpClient("travelcard-token", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddSingleton<SecretsHelper>();
        services.AddSingleton<ITravelcardDbConnection, Service>();
        services.AddSingleton<ITravelcardDbService, Service>();
        services.AddSingleton<Service>();
        ServiceProvider = services.BuildServiceProvider();
    }

    public async Task<APIGatewayProxyResponse> httpcsharplambda(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var logger = ServiceProvider.GetRequiredService<ILogger<Function>>();
        var clientIdHeader = GetHeaderValue(request.Headers, "client_id");
        logger.LogInformation("Controller entry: method={Method}, route={Route}, client_id={ClientId}", request.HttpMethod, request.Path, clientIdHeader);

        try
        {
            var service = ServiceProvider.GetRequiredService<ITravelcardDbService>();
            var response = await service.ProcessAsync(request, CancellationToken.None);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in controller");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
                Body = System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message })
            };
        }
    }

    private static string? GetHeaderValue(IDictionary<string, string>? headers, string name)
    {
        if (headers == null)
        {
            return null;
        }

        foreach (var entry in headers)
        {
            if (string.Equals(entry.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Value;
            }
        }

        return null;
    }
}