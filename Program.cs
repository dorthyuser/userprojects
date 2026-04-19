using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Net.Http.Headers;
using ZohoCrmOauthFinal1.Services;

var builder = WebApplication.CreateBuilder(args);

// Memory cache (for token caching)
builder.Services.AddMemoryCache();

builder.Services.AddControllers();

// -----------------------------
// Key Vault Setup
// -----------------------------
var kvUrl = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT");
if (string.IsNullOrEmpty(kvUrl))
    throw new InvalidOperationException("Env var 'AZURE_KEY_VAULT' is not set");

var secretClient = new SecretClient(new Uri(kvUrl), new DefaultAzureCredential());
builder.Services.AddSingleton(secretClient);

// -----------------------------
// HttpClient (Zoho CRM)
// -----------------------------
builder.Services.AddHttpClient("zoho-crm-connection")
    .ConfigureHttpClient((sp, client) =>
    {
        // ✅ BASE URL from ENV (NOT Key Vault)
        var baseUrl = Environment.GetEnvironmentVariable("ZOHO_BASE_URL");
        if (string.IsNullOrEmpty(baseUrl))
            throw new InvalidOperationException("Env var 'ZOHO_BASE_URL' is not set");

        client.BaseAddress = new Uri(baseUrl);

        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    });

// -----------------------------
// Services
// -----------------------------
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();

// Logging
builder.Services.AddLogging();

// -----------------------------
// Kestrel (Port binding)
// -----------------------------
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

var app = builder.Build();

app.MapControllers();

app.Run();
