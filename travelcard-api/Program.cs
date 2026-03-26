using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using TravelcardApi.Helpers;
using TravelcardApi.Models;
using TravelcardApi.Services;

var host = new HostBuilder()
    .ConfigureAppConfiguration(cfg =>
    {
        cfg.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddLogging(builder => builder.AddConsole());

        var configuration = context.Configuration;
        var conn = configuration["PostgresConnectionString"] ?? "Host=localhost;Username=postgres;Password=postgres;Database=travelcardsdb";

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(conn);
        // Map enums to Postgres enum types
        dataSourceBuilder.MapEnum<TravelcardType>("travelcard_type_enum");
        dataSourceBuilder.MapEnum<CardholderType>("cardholder_type_enum");

        var dataSource = dataSourceBuilder.Build();

        services.AddSingleton(dataSource);
        services.AddSingleton<DatabaseHelper>();
        services.AddScoped<TravelcardService>();
    })
    .UseConsoleLifetime()
    .Build();

await host.RunAsync();
