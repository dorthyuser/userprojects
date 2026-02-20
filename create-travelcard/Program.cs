using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using CreateTravelcard.Helpers;
using CreateTravelcard.Validators;
using Microsoft.Extensions.Logging;

namespace CreateTravelcard
{
    public class Program
    {
        public static async Task Main()
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration(builder =>
                {
                    builder.AddEnvironmentVariables();
                })
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<TokenGenerator>();
                    services.AddSingleton<TravelcardValidator>();
                    services.AddSingleton<DbHelper>();
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.AddConsole();
                })
                .Build();

            await host.RunAsync();
        }
    }
}
