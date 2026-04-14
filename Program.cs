using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace tc_csharp_api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Ensure configuration can resolve environment variables
            builder.Configuration.AddEnvironmentVariables();

            // Logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            // Register services
            builder.Services.AddControllers();

            // Http client used by TokenService to call token endpoint
            builder.Services.AddHttpClient("token-client");

            // Token service and secret provider
            builder.Services.AddSingleton<ISecretProvider, SecretProvider>();
            builder.Services.AddSingleton<ITokenService, TokenService>();

            // Register OAuthTokenHandler and sb-connection client
            builder.Services.AddTransient<OAuthTokenHandler>();
            builder.Services.AddSbConnection();

            // Application services
            builder.Services.AddScoped<ITravelcardService, TravelcardService>();

            var app = builder.Build();

            // Ensure the application listens on HTTPS port 8080
            var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
            if (string.IsNullOrEmpty(urls))
            {
                // UseUrls with HTTPS on port 8080
                app.Urls.Add("https://0.0.0.0:8080");
            }

            app.MapControllers();

            app.Run();
        }
    }
}
