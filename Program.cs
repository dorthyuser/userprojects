using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using tc_testing_api2.Services;

var builder = WebApplication.CreateBuilder(args);

// Load configuration from environment as required
builder.Configuration.AddEnvironmentVariables();

// Add services
builder.Services.AddControllers();
builder.Services.AddLogging();

// Register connection client and services
builder.Services.AddSingleton<TcConnectionClient>();
builder.Services.AddScoped<ITravelcardService, TravelcardService>();

// Token handler and HttpClients
builder.Services.AddTransient<TokenDelegatingHandler>();

builder.Services.AddHttpClient("token-client");

builder.Services.AddHttpClient("tc-connection")
    .AddHttpMessageHandler<TokenDelegatingHandler>();

var app = builder.Build();

app.MapControllers();

// Read port from configuration or default to 8080
var portValue = builder.Configuration["Port"] ?? builder.Configuration["PORT"] ?? "8080";
if (!int.TryParse(portValue, out var port))
{
    port = 8080;
}

var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
logger.LogInformation("Starting application on port {Port}", port);

// Run with HTTPS endpoint on configured port. Use default certificate resolution (development cert or configured cert).
app.Run($"https://0.0.0.0:{port}");
