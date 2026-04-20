using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ZohoProject3.Models;
using ZohoProject3.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// Initialise SecretClient once at startup
var kvUrl = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT");
if (string.IsNullOrEmpty(kvUrl))
    throw new InvalidOperationException("Env var 'AZURE_KEY_VAULT' is not set");

var secretClient = new SecretClient(new Uri(kvUrl), new DefaultAzureCredential());

// Resolve secrets using two-step lookup
async Task<string> ResolveSecretAsync(string envVarName)
{
    var keyName = Environment.GetEnvironmentVariable(envVarName);
    if (string.IsNullOrEmpty(keyName)) throw new InvalidOperationException($"Env var '{envVarName}' is not set");
    var secret = await secretClient.GetSecretAsync(keyName);
    return secret.Value.Value ?? throw new InvalidOperationException($"Secret '{keyName}' returned null");
}

// Fetch required secrets
var clientId = await ResolveSecretAsync("ZOHO-CLIENT-ID");
var clientSecret = await ResolveSecretAsync("ZOHO-CLIENT-SECRET");
var refreshToken = await ResolveSecretAsync("ZOHO-REFRESH-TOKEN");
var tokenUrl = await ResolveSecretAsync("ZOHO_TOKEN_URL");
var baseUrl = await ResolveSecretAsync("ZOHO_BASE_URL");

// Normalize base URL
baseUrl = baseUrl.TrimEnd('/');

var zohoOptions = new ZohoOptions
{
    ClientId = clientId ?? throw new InvalidOperationException("ClientId not configured"),
    ClientSecret = clientSecret ?? throw new InvalidOperationException("ClientSecret not configured"),
    RefreshToken = refreshToken ?? throw new InvalidOperationException("RefreshToken not configured"),
    TokenUrl = tokenUrl ?? throw new InvalidOperationException("TokenUrl not configured"),
    BaseUrl = baseUrl ?? throw new InvalidOperationException("BaseUrl not configured"),
    Scopes = "ZohoCRM.users.ALL"
};

// Register SecretClient as singleton (but not injected into connection)
builder.Services.AddSingleton(secretClient);
// Register resolved options as singleton
builder.Services.AddSingleton(zohoOptions);

// Register named HttpClients
builder.Services.AddHttpClient("zoho-api-client", client =>
{
    client.BaseAddress = new Uri(zohoOptions.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("zoho-token-client", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    // IMPORTANT: do NOT set BaseAddress for token client
});

// DI registrations
builder.Services.AddSingleton<IZohoCrmConnection, ZohoCrmConnection>();
builder.Services.AddScoped<IZohoCrmService, ZohoCrmService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

await app.RunAsync();
