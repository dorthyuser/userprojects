using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TravelcardFunctionApp.Helpers;
using Microsoft.Extensions.Logging;

namespace TravelcardFunctionApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder.AddEnvironmentVariables();
                })
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((context, services) =>
                {
                    services.AddHttpClient<IHttpHelper, HttpHelper>();
                    services.AddHttpClient<ITravelcardService, TravelcardService>();

                    services.AddSingleton<IHttpHelper, HttpHelper>();
                    services.AddSingleton<ITravelcardService, TravelcardService>();
                })
                .ConfigureLogging((context, b) =>
                {
                    b.AddConsole();
                })
                .Build();

            host.Run();
        }
    }
}
