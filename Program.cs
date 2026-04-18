using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Net.Http.Headers;
using ZohoCrmOauthFinal1.Services;

var builder = WebApplication.CreateBuilder(args);

// Required for token caching
builder.Services.AddMemoryCache();

builder.Services.AddControllers();

// Initialise SecretClient (TWO-STEP initialisation)
var kvUrl = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT");
if (string.IsNullOrEmpty(kvUrl))
    throw new InvalidOperationException("Env var 'AZURE_KEY_VAULT' is not set");

var secretClient = new SecretClient(new Uri(kvUrl), new DefaultAzureCredential());
builder.Services.AddSingleton(secretClient);

// HttpClient named "zoho-crm-connection" — base URL resolved from Key Vault using TWO-STEP resolution
builder.Services.AddHttpClient("zoho-crm-connection").ConfigureHttpClient((sp, client) =>
{
    var sc = sp.GetRequiredService<SecretClient>();
    var baseKey = Environment.GetEnvironmentVariable("ZOHO-BASE-URL");
    if (string.IsNullOrEmpty(baseKey))
        throw new InvalidOperationException("Env var 'ZOHO-BASE-URL' is not set");

    var baseUrl = sc.GetSecretAsync(baseKey).GetAwaiter().GetResult().Value.Value;
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

// Register services
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();

// Logging
builder.Services.AddLogging();

// Configure Kestrel BEFORE Build()
builder.WebHost.ConfigureKestrel(options => { options.ListenAnyIP(8080); });

var app = builder.Build();

app.MapControllers();

app.Run();
