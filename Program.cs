using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using zoho_project_csharp.Models;
using zoho_project_csharp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// Initialise SecretClient once at startup
var kvUrl = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT");
if (string.IsNullOrEmpty(kvUrl))
    throw new InvalidOperationException("Env var 'AZURE_KEY_VAULT' is not set");

var secretClient = new SecretClient(new Uri(kvUrl), new DefaultAzureCredential());
builder.Services.AddSingleton(secretClient);

// Resolve required secrets ONCE at startup using two-step lookup
var clientIdKey = Environment.GetEnvironmentVariable("ZOHO_CLIENT_ID") ?? throw new InvalidOperationException("Env var 'ZOHO_CLIENT_ID' is not set");
var clientId = (await secretClient.GetSecretAsync(clientIdKey)).Value.Value ?? throw new InvalidOperationException("ZOHO_CLIENT_ID secret empty");

var clientSecretKey = Environment.GetEnvironmentVariable("ZOHO_CLIENT_SECRET") ?? throw new InvalidOperationException("Env var 'ZOHO_CLIENT_SECRET' is not set");
var clientSecret = (await secretClient.GetSecretAsync(clientSecretKey)).Value.Value ?? throw new InvalidOperationException("ZOHO_CLIENT_SECRET secret empty");

var tokenUrlKey = Environment.GetEnvironmentVariable("ZOHO_TOKEN_URL") ?? throw new InvalidOperationException("Env var 'ZOHO_TOKEN_URL' is not set");
var tokenUrl = (await secretClient.GetSecretAsync(tokenUrlKey)).Value.Value ?? throw new InvalidOperationException("ZOHO_TOKEN_URL secret empty");

var refreshTokenKey = Environment.GetEnvironmentVariable("ZOHO_REFRESH_TOKEN") ?? throw new InvalidOperationException("Env var 'ZOHO_REFRESH_TOKEN' is not set");
var refreshToken = (await secretClient.GetSecretAsync(refreshTokenKey)).Value.Value ?? throw new InvalidOperationException("ZOHO_REFRESH_TOKEN secret empty");

var baseUrlKey = Environment.GetEnvironmentVariable("ZOHO_BASE_URL") ?? throw new InvalidOperationException("Env var 'ZOHO_BASE_URL' is not set");
var baseUrlRaw = (await secretClient.GetSecretAsync(baseUrlKey)).Value.Value ?? throw new InvalidOperationException("ZOHO_BASE_URL secret empty");
var baseUrl = baseUrlRaw.TrimEnd('/');

var scopes = "ZohoCRM.users.ALL";

var zohoOptions = new ZohoOptions
{
    ClientId = clientId ?? throw new InvalidOperationException("ClientId not configured"),
    ClientSecret = clientSecret ?? throw new InvalidOperationException("ClientSecret not configured"),
    TokenUrl = tokenUrl ?? throw new InvalidOperationException("TokenUrl not configured"),
    RefreshToken = refreshToken ?? throw new InvalidOperationException("RefreshToken not configured"),
    BaseUrl = baseUrl ?? throw new InvalidOperationException("BaseUrl not configured"),
    Scopes = scopes,
    Port = 8080,
    Provider = "AZURE"
};

builder.Services.AddSingleton(zohoOptions);

// HTTP clients: API client (with fallback BaseAddress) and token client (no BaseAddress)
builder.Services.AddHttpClient("zoho-api", c =>
{
    c.Timeout = TimeSpan.FromSeconds(30);
    c.BaseAddress = new Uri(zohoOptions.BaseUrl);
});

builder.Services.AddHttpClient("zoho-token", c =>
{
    c.Timeout = TimeSpan.FromSeconds(30);
    // Intentionally no BaseAddress for token client
});

// DI registrations
builder.Services.AddSingleton<IZohoCrmConnection, ZohoCrmConnection>();
builder.Services.AddScoped<IZohoCrmService, ZohoCrmService>();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
