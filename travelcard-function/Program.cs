using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Extensions.Logging;
using TravelcardFunction.Helpers;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(config =>
    {
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        var connString = configuration.GetValue<string>("PostgresConnectionString") ?? "Host=localhost;Username=postgres;Password=postgres;Database=travelcards;Pooling=true";
        services.AddSingleton(sp => new DatabaseHelper(connString, sp.GetRequiredService<ILoggerFactory>()));
    })
    .ConfigureLogging(builder => builder.AddConsole())
    .Build();

host.Run();
