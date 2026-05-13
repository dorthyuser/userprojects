using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TravelcardFunctionApp.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<TravelcardDbHelper>();
    })
    .Build();

await host.RunAsync();