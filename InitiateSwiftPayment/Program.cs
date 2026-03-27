using System;
using InitiateSwiftPayment.Helpers;
using InitiateSwiftPayment.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InitiateSwiftPayment
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
                    var configuration = context.Configuration;
                    var conn = configuration["PostgresConnectionString"] ?? "Host=localhost;Username=postgres;Password=postgres;Database=bankdb";

                    services.AddSingleton(new DbHelper(conn));
                    services.AddSingleton(new BicDirectory());
                    services.AddSingleton(new CbsClient());
                    services.AddSingleton<PaymentService>();
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.AddConsole();
                })
                .Build();

            host.Run();
        }
    }
}
