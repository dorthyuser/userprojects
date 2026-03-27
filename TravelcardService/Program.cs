using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TravelcardService.Helpers;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder.AddEnvironmentVariables();
    })
    .ConfigureServices(svcs =>
    {
        svcs.AddSingleton<DbHelper>();
        svcs.AddSingleton<Validator>();
    })
    .ConfigureLogging(lb => lb.AddConsole())
    .Build();

host.Run();
