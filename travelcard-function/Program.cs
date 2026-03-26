using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TravelcardFunction.Helpers;

namespace TravelcardFunction
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration((context, builder) => { builder.AddEnvironmentVariables(); })
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((context, services) =>
                {
                    var config = context.Configuration;
                    var conn = config.GetValue<string>("PostgresConnectionString") ?? "Host=localhost;Username=postgres;Password=postgres;Database=travelcards;Pooling=true";
                    services.AddSingleton<PostgresDb>(sp =>
                    {
                        var logger = sp.GetRequiredService<ILogger<PostgresDb>>();
                        return new PostgresDb(conn, logger);
                    });
                })
                .Build();

            await host.RunAsync();
        }
    }
}
