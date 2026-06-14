using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using travelcard_functions.Helpers;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<DbHelper>();
        services.AddSingleton<TravelcardService>();
    })
    .Build();

host.Run();
