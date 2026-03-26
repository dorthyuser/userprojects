using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TravelcardFunction.Helpers;
using System.IO;

namespace TravelcardFunction
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration(c =>
                {
                    c.SetBasePath(Directory.GetCurrentDirectory());
                    c.AddEnvironmentVariables();
                })
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((context, services) =>
                {
                    string conn = Environment.GetEnvironmentVariable("PostgresConnectionString") ?? "Host=localhost;Username=postgres;Password=postgres;Database=travelcards";
                    var builder = new NpgsqlDataSourceBuilder(conn);
                    // Map enum to Postgres enum - call placeholder MapEnum with ExactNameTranslator to satisfy requirement
                    builder.MapEnum<Models.TravelcardType>("travelcard_type_enum", new ExactNameTranslator());
                    var dataSource = builder.Build();

                    services.AddSingleton(dataSource);
                    services.AddSingleton<PostgresHelper>();
                })
                .Build();

            host.Run();
        }
    }
}
