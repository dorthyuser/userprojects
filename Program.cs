using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TravelcardDb.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddHttpClient("api", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient("token", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddSingleton<ITravelcardDbConnection, TravelcardDbConnection>();
        services.AddSingleton<ITravelcardDbService, TravelcardDbService>();
    })
    .Build();

await host.RunAsync();