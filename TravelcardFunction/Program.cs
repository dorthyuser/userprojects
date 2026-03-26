using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TravelcardFunction.Helpers;
using System;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Register DbHelper as singleton with connection string from env
        var connString = Environment.GetEnvironmentVariable("PostgresConnectionString") ?? "Host=localhost;Username=postgres;Password=postgres;Database=travelcards";
        services.AddSingleton<DbHelper>(sp => new DbHelper(connString, sp.GetRequiredService<ILogger<DbHelper>>()));
    })
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
    })
    .Build();

host.Run();
