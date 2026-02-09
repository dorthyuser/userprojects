using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Npgsql.Logging;

using TravelCardFunctionApp.Data;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        var connString = configuration["PostgresConnectionString"];
        if (string.IsNullOrWhiteSpace(connString))
        {
            throw new InvalidOperationException("PostgresConnectionString is not configured.");
        }

        // Build an NpgsqlDataSource with pooling
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connString);
        var dataSource = dataSourceBuilder.Build();

        services.AddSingleton(dataSource);

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            // Create a connection from the pooled datasource per scope
            var ds = sp.GetRequiredService<NpgsqlDataSource>();
            var conn = ds.CreateConnection();
            options.UseNpgsql(conn);
        }, ServiceLifetime.Scoped);

        services.AddScoped<TravelCardFunctionApp.Repositories.TravelCardRepository>();

    })
    .ConfigureLogging((context, logging) =>
    {
        logging.AddConsole();
    })
    .Build();

host.Run();