using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AgeApi.Data;
using AgeApi.Services;
using AgeApi.Enums;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // Configure DbContext using InMemory for local/in-memory storage
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("AgeApiDb"));

        // Register services
        services.AddSingleton<IAuthService, ApiKeyAuthService>();

        // Configure JSON serializer options globally
        services.AddSingleton(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        });
    })
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
    })
    .Build();

await host.RunAsync();