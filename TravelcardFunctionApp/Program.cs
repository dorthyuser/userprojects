using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TravelcardFunctionApp.Helpers;

namespace TravelcardFunctionApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureAppConfiguration((context, builder) => { builder.AddEnvironmentVariables(); })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<DbHelper>();
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
