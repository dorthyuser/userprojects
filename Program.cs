using System;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ZohoCrmOauthFinal.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddLogging();
builder.Services.AddHttpClient("zoho-crm-connection");

// Step 1 — read env var to get the actual vault URL:
var kvUrl = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT");
if (string.IsNullOrEmpty(kvUrl))
    throw new InvalidOperationException("Env var 'AZURE_KEY_VAULT' is not set");

// Step 2 — use the resolved URL to create the secret client:
builder.Services.AddSingleton(sp => new SecretClient(new Uri(kvUrl), new DefaultAzureCredential()));

// Register application services
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IZohoUserService, ZohoUserService>();

// Configure Kestrel BEFORE Build()
builder.WebHost.ConfigureKestrel(options => { options.ListenAnyIP(8080); });

var app = builder.Build();

// Configure the HTTP request pipeline
app.MapControllers();

app.Run();
