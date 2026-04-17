using System;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZohoOauthKvTest.Services;

var builder = WebApplication.CreateBuilder(args);

// Add logging
builder.Services.AddLogging(config => config.AddConsole());

// Step 1 — read env var to get the actual vault URL:
var kvUrl = Environment.GetEnvironmentVariable("AZURE_KEY_URL");
if (string.IsNullOrEmpty(kvUrl))
    throw new InvalidOperationException("Env var 'AZURE_KEY_URL' is not set");

// Step 2 — use the resolved URL to create the secret client:
var secretClient = new SecretClient(new Uri(kvUrl), new DefaultAzureCredential());
builder.Services.AddSingleton(secretClient);

// Resolve Zoho Base URL from Key Vault using TWO-STEP secret resolution
var baseUrlKey = Environment.GetEnvironmentVariable("ZOHO_BASE_URL");
if (string.IsNullOrEmpty(baseUrlKey))
    throw new InvalidOperationException("Env var 'ZOHO_BASE_URL' is not set");
var baseUrl = secretClient.GetSecret(baseUrlKey).Value.Value;

// Register HttpClient for Zoho API
builder.Services.AddHttpClient("Zoho", client =>
{
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(100);
});

// Add default HttpClient for token operations
builder.Services.AddHttpClient();

// Register services
builder.Services.AddSingleton<ITokenService, ZohoTokenService>();
builder.Services.AddScoped<IZohoHttpService, ZohoHttpService>();

builder.Services.AddControllers();

// Configure Kestrel BEFORE Build()
builder.WebHost.ConfigureKestrel(options => { options.ListenAnyIP(8080); });

var app = builder.Build();

app.MapControllers();

app.Run();
