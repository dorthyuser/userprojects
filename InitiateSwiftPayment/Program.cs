using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using InitiateSwiftPayment.Helpers;

namespace InitiateSwiftPayment
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder.AddEnvironmentVariables();
                })
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<DbHelper>(sp => new DbHelper(sp.GetRequiredService<IConfiguration>()));
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
