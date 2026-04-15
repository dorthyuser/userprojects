using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using hello_http_test.Services;
using hello_http_test.Data;
using System.Net.Http.Headers;

namespace hello_http_test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configuration and logging
            var configuration = builder.Configuration;
            var loggerFactory = LoggerFactory.Create(logging => logging.AddConsole());

            // Ensure application port from appsettings or env
            var portVal = configuration["ApplicationPort"] ?? Environment.GetEnvironmentVariable("ApplicationPort") ?? "8080";

            // Register Zoho auth and token handler
            builder.Services.AddSingleton<ZohoAuthService>();
            builder.Services.AddTransient<TokenHandler>();

            // Register the named HTTP client for the Zoho connection
            builder.Services.AddHttpClient<ZohoTestHttpConClient>(client =>
            {
                var baseUrl = configuration["ZOHO_store_api_url"] ?? Environment.GetEnvironmentVariable("ZOHO_store_api_url");
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    throw new InvalidOperationException("ZOHO_store_api_url is not configured in IConfiguration or environment variables.");
                }
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }).AddHttpMessageHandler<TokenHandler>();

            // Register higher-level service and repository
            builder.Services.AddScoped<IZohoStoreService, ZohoStoreService>();
            builder.Services.AddScoped<IZohoStoreRepository, ZohoStoreRepository>();

            // Add controllers
            builder.Services.AddControllers();

            var app = builder.Build();

            app.MapControllers();

            // Configure Kestrel to listen on provided port using HTTPS
            app.Urls.Clear();
            app.Urls.Add($"https://0.0.0.0:{portVal}");

            app.Run();
        }
    }
}
