using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AccountsFunction.Helpers;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
 .ConfigureFunctionsWorkerDefaults()
 .ConfigureServices(services =>
 {
 // Database helper is an in-memory store for demo/testing purposes.
 services.AddSingleton<DatabaseHelper>();
 })
 .Build();

await host.RunAsync();
