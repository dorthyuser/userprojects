using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using InitiateSwiftPayment.Helpers;
using InitiateSwiftPayment.Services;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureAppConfiguration(config =>
    {
        config.AddEnvironmentVariables();
    })
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        var conn = configuration["PostgresConnectionString"] ?? "Host=localhost;Username=postgres;Password=postgres;Database=bankdb";
        services.AddSingleton(new DbHelper(conn));
        services.AddSingleton<PaymentService>();
    })
    .Build();

host.Run();
