using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using TravelcardFunction.Helpers;
using System.IO;
using TravelcardFunction.Models;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(config =>
    {
        config.SetBasePath(Directory.GetCurrentDirectory());
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        var connStr = configuration["PostgresConnectionString"] ?? "Host=localhost;Username=postgres;Password=postgres;Database=travelcards";

        var builder = new NpgsqlDataSourceBuilder(connStr);
        // Map enums to Postgres enum type name using available overloads.
        try
        {
            builder.MapEnum<TravelcardType>("travelcard_type_enum");
            builder.MapEnum<CardholderType>("cardholder_type_enum");
        }
        catch { /* best-effort map for environments where MapEnum overload differs */ }

        var dataSource = builder.Build();

        services.AddSingleton(dataSource);
        services.AddSingleton<IDbHelper, DbHelper>();
    
    })
    .ConfigureLogging((context, b) =>
    {
        b.AddConsole();
    })
    .Build();

await host.RunAsync();
