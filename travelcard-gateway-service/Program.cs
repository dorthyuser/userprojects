using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using TravelcardGatewayService.Helpers;

namespace TravelcardGatewayService
{
public class Program
{
public static void Main(string[] args)
{
var host = new HostBuilder()
.ConfigureFunctionsWorkerDefaults()
.ConfigureServices((context, services) =>
{
// Token service (uses HttpClient internally)
services.AddHttpClient<TokenService>();
                // Typed HttpClient for backend API
                services.AddHttpClient<ITravelcardHttpClient, TravelcardHttpClient>(client =>
                {
                    var baseUrl = Environment.GetEnvironmentVariable("TRAVELCARD_API_URL");

                    if (string.IsNullOrWhiteSpace(baseUrl))
                    {
                        throw new Exception("TRAVELCARD_API_URL is not configured");
                    }

                    client.BaseAddress = new Uri(baseUrl);
                    client.Timeout = TimeSpan.FromSeconds(30);
                });
            })
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
            })
            .Build();

        host.Run();
    }
}
}
