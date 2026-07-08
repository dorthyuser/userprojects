using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AdverseEventReporter.Helpers;
using AdverseEventReporter.Services;

namespace AdverseEventReporter.Functions;

public static class FunctionEntry
{
    public static IHostBuilder ConfigureApp(this IHostBuilder builder)
    {
        return builder.ConfigureFunctionsWorkerDefaults().ConfigureServices(services =>
        {
            services.AddSingleton<DbHelper>();
            services.AddSingleton<AdverseEventService>();
        });
    }
}
