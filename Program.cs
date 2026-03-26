using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using ResponseHttp.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace ResponseHttp
{
    /// <summary>
    /// Program entry for the ASP.NET Core application. Configures DI, HTTP client connection, and hosts the Web API.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Ensure application uses configuration from appsettings.json and environment.
            var configuration = builder.Configuration;

            // Register controllers
            builder.Services.AddControllers();

            // Register our response service
            builder.Services.AddScoped<IResponseService, ResponseService>();

            // Register the configured HTTP connection using the extension.
            builder.Services.AddHttpConnection(configuration);

            // Configure logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            var app = builder.Build();

            app.MapControllers();

            // Use application port from configuration if provided, default to 8080 as required.
            var port = configuration["Application:Port"] ?? "8080";
            var url = configuration["Application:Url"] ?? $"https://0.0.0.0:{port}";

            // Start the app on the configured URL.
            app.Logger.LogInformation("Starting application on {Url}", url);
            app.Urls.Clear();
            app.Urls.Add(url);

            app.Run();
        }
    }
}
