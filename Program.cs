using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ZohoProject2.Models;
using ZohoProject2.Services;

var builder = WebApplication.CreateBuilder(args);

// Load optional appsettings.json per rule
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// Initialize SecretClient once at startup
var kvUrl = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT");
if (string.IsNullOrEmpty(kvUrl))
    throw new InvalidOperationException("Env var 'AZURE_KEY_VAULT' is not set");

var secretClient = new SecretClient(new Uri(kvUrl), new DefaultAzureCredential());

// Resolve required secret keys (two-step lookups)
async Task<string> ResolveSecretAsync(string envVarName)
{
    var secretKeyName = Environment.GetEnvironmentVariable(envVarName);
    if (string.IsNullOrEmpty(secretKeyName))
        throw new InvalidOperationException($"Env var '{envVarName}' is not set");

    var secret = await secretClient.GetSecretAsync(secretKeyName).ConfigureAwait(false);
    return secret.Value.Value ?? throw new InvalidOperationException($"Secret '{secretKeyName}' returned empty value");
}

// Resolve secrets
var baseUrl = await ResolveSecretAsync("ZOHO_BASE_URL").ConfigureAwait(false);
baseUrl = baseUrl.TrimEnd('/');

var clientId = await ResolveSecretAsync("ZOHO-CLIENT-ID").ConfigureAwait(false);
var clientSecret = await ResolveSecretAsync("ZOHO-CLIENT-SECRET").ConfigureAwait(false);
var tokenUrl = await ResolveSecretAsync("ZOHO_TOKEN_URL").ConfigureAwait(false);
var refreshToken = await ResolveSecretAsync("ZOHO-REFRESH-TOKEN").ConfigureAwait(false);

// Register SecretClient singleton
builder.Services.AddSingleton(secretClient);

// Register ZohoOptions (single source of truth)
var zohoOptions = new ZohoOptions
{
    ClientId = clientId ?? throw new InvalidOperationException("ClientId not configured"),
    ClientSecret = clientSecret ?? throw new InvalidOperationException("ClientSecret not configured"),
    TokenUrl = tokenUrl ?? throw new InvalidOperationException("TokenUrl not configured"),
    RefreshToken = refreshToken ?? throw new InvalidOperationException("RefreshToken not configured"),
    BaseUrl = baseUrl ?? throw new InvalidOperationException("BaseUrl not configured"),
    Port = builder.Configuration.GetValue<int>("Application:Port", 8080),
    Provider = builder.Configuration.GetValue<string>("Application:Provider", "AZURE") ?? "AZURE"
};

builder.Services.AddSingleton(zohoOptions);

// Configure named HttpClients per rules
builder.Services.AddHttpClient("zoho_api", client =>
{
    client.BaseAddress = new Uri(zohoOptions.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("zoho_token", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    // Intentionally do NOT set BaseAddress for token client
});

// Register application services
builder.Services.AddSingleton<IZohoCrmConnection, ZohoCrmConnection>();
builder.Services.AddScoped<IZohoCrmService, ZohoCrmService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

await app.RunAsync();
