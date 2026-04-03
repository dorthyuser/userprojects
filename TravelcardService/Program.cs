using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TravelcardService.Helpers;

var host = new HostBuilder()
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder.AddEnvironmentVariables();
    })
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddHttpClient("travelcard-client");
        services.AddSingleton<IKeyVaultService, KeyVaultService>();
        services.AddScoped<IHttpHelper, HttpHelper>();
    })
    .ConfigureLogging((context, b) =>
    {
        b.AddConsole();
    })
    .Build();

host.Run();
