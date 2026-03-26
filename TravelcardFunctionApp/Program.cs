using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TravelcardFunctionApp.Helpers;

var host = new HostBuilder()
    .ConfigureAppConfiguration(config =>
    {
        config.AddEnvironmentVariables();
    })
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;
        var conn = config["PostgresConnectionString"] ?? "Host=localhost;Username=postgres;Password=postgres;Database=travelcards;Pooling=true";
        services.AddSingleton(sp => new DbHelper(conn, sp.GetRequiredService<ILogger<DbHelper>>()));
    })
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
    })
    .Build();

host.Run();
