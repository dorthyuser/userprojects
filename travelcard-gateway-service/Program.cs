using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Configuration;
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
                    services.AddHttpClient<TokenService>();

                    services.AddHttpClient<TravelcardHttpClient>(client =>
                    {
                        var baseUrl = Environment.GetEnvironmentVariable("TRAVELCARD_API_URL") ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(baseUrl))
                        {
                            client.BaseAddress = new Uri(baseUrl);
                        }
                    });

                    services.AddSingleton<TokenService>(sp =>
                    {
                        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                        var client = httpClientFactory.CreateClient(nameof(TokenService));
                        var logger = sp.GetRequiredService<ILogger<TokenService>>();
                        return new TokenService(client, logger);
                    });

                    services.AddSingleton<ITravelcardHttpClient>(sp =>
                    {
                        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                        var client = httpClientFactory.CreateClient(typeof(TravelcardHttpClient).FullName);
                        var tokenService = sp.GetRequiredService<TokenService>();
                        var logger = sp.GetRequiredService<ILogger<TravelcardHttpClient>>();
                        return new TravelcardHttpClient(client, tokenService, logger);
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
