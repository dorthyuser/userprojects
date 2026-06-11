using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using create_travelcard_prod.Helpers;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<DbHelper>();
        services.AddSingleton<TravelcardRepository>();
    })
    .Build();

host.Run();
