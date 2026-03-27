using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Configuration;
using TravelcardFunction.Helpers;
using TravelcardFunction.Models;

namespace TravelcardFunction
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration((context, b) =>
                {
                    b.AddEnvironmentVariables();
                })
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<DbHelper>(sp =>
                    {
                        var config = sp.GetService<IConfiguration>();
                        var conn = config?["PostgresConnectionString"] ?? "Host=localhost;Username=postgres;Password=postgres;Database=travelcards_db";
                        var logger = sp.GetService<ILogger<DbHelper>>();
                        return new DbHelper(conn, logger!);
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
