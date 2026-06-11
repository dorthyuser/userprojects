using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace life_time_calculator;

public class Program
{
    public static void Main(string[] args)
    {
        HostBuilder builder = new HostBuilder();
        builder.ConfigureFunctionsWorkerDefaults();
        builder.ConfigureServices(services =>
        {
        });
        builder.Build().Run();
    }
}
