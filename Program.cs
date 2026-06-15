using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using travelcard_function_app.Helpers;
using travelcard_function_app.Services;
using travelcard_function_app.Validation;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<DbHelper>();
        services.AddSingleton<TravelcardRepository>();
        services.AddSingleton<TravelcardService>();
        services.AddSingleton<RequestValidator>();
    })
    .Build();

host.Run();
