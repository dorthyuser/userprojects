using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Configuration;
using travelcardservice.Helpers;

var host = new HostBuilder()
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddEnvironmentVariables();
    })
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<DbHelper>();
    })
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
    })
    .Build();

host.Run();
