using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker.Configuration;
using Npgsql;

namespace AccountsFunction
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration((context, builder) =>
                {
                    // local.settings.json for local dev, ignored in Azure
                    builder.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                           .AddEnvironmentVariables();
                })
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((context, services) =>
                {
                    var config = context.Configuration;

                    // Must exist in Azure App Settings or local.settings.json
                    var connString =
                        config.GetConnectionString("PostgresConnectionString")
                        ?? config["PostgresConnectionString"];

                    if (string.IsNullOrWhiteSpace(connString))
                        throw new InvalidOperationException("PostgresConnectionString must be provided via configuration.");

                    var npgsqlBuilder = new NpgsqlDataSourceBuilder(connString);
                    var dataSource = npgsqlBuilder.Build();

                    services.AddSingleton(dataSource);
                    services.AddSingleton<Helpers.DatabaseHelper>();
                })
                .Build();

            await host.RunAsync();
        }
    }
}
