using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Azure.Functions.Worker;
using travelcard_http.Helpers;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddLogging();
        services.AddSingleton<DbHelper>();

        // Register OAuth delegating handler and an HttpClient that uses it
        services.AddTransient<OAuthDelegatingHandler>();
        services.AddHttpClient("azure-function-oauth").AddHttpMessageHandler<OAuthDelegatingHandler>();

        // Make IConfiguration available
        services.AddSingleton<IConfiguration>(context.Configuration);
    })
    .Build();

host.Run();
