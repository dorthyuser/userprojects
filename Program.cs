using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TravelCardFunctionApp.Helpers;
using TravelCardFunctionApp.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<DbHelper>();
        services.AddSingleton<TravelCardService>();
    })
    .Build();

host.Run();
