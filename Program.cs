using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using hello_http_test.Services;
using hello_http_test.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add configuration from appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Read the Key Vault URL from environment variable AZURE_KV_URL
var vaultUrl = Environment.GetEnvironmentVariable("AZURE_KV_URL");
if (string.IsNullOrEmpty(vaultUrl))
{
    throw new InvalidOperationException("Environment variable AZURE_KV_URL must be set to the Key Vault URL.");
}

var vaultUri = new Uri(vaultUrl);

// Register SecretClient using DefaultAzureCredential (supports managed identity in Azure)
builder.Services.AddSingleton(new SecretClient(vaultUri, new DefaultAzureCredential()));

// Register HttpClientFactory and named client for zoho connection
builder.Services.AddHttpClient("zoho-test-http-con");

// DI registrations
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IZohoService, ZohoService>();
builder.Services.AddSingleton<ZohoHttpClientProvider>();

builder.Services.AddControllers();

var app = builder.Build();

// Ensure hosted on requested port (HTTPS)
var port = builder.Configuration.GetSection("Hosting")?.GetValue<int>("Port") ?? 8080;
var url = $"https://0.0.0.0:{port}";
app.Urls.Clear();
app.Urls.Add(url);

app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
