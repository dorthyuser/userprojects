using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Configuration;
using travelcard_service.Helpers;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(svc =>
    {
        svc.AddHttpClient<OAuthTokenService>();
        svc.AddHttpClient<HttpHelper>();
        svc.AddSingleton<OAuthTokenService>();
        svc.AddSingleton<HttpHelper>();
    })
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
    })
    .Build();

await host.RunAsync();
