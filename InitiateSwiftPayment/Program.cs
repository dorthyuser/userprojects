using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using InitiateSwiftPayment.Helpers;
using InitiateSwiftPayment.Services;
using Microsoft.Azure.Functions.Worker.Configuration;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(config =>
    {
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        var conn = configuration["PostgresConnectionString"] ?? "Host=localhost;Username=postgres;Password=postgres;Database=bankdb";
        services.AddSingleton(new DbHelper(conn));
        services.AddScoped<IPaymentService, PaymentService>();
    })
    .ConfigureLogging((context, logging) =>
    {
        logging.AddConsole();
    })
    .Build();

host.Run();
