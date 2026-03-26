using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Configuration;
using travelcard_function.Helpers;

namespace travelcard_function
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureAppConfiguration(config => { config.AddEnvironmentVariables(); })
                .ConfigureServices((context, services) =>
                {
                    services.AddLogging();
                    var configuration = context.Configuration;
                    var conn = configuration["PostgresConnectionString"] ?? "Host=localhost;Username=postgres;Password=password;Database=travelcardsdb";
                    services.AddSingleton<IDbHelper>(sp => new DbHelper(conn, sp.GetRequiredService<ILoggerFactory>()));
                })
                .Build();

            host.Run();
        }
    }
}
