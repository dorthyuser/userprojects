using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ZohoProject1.Models;
using ZohoProject1.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// Initialise SecretClient once
var kvUrl = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT");
if (string.IsNullOrEmpty(kvUrl))
    throw new InvalidOperationException("Env var 'AZURE_KEY_VAULT' is not set");

var secretClient = new SecretClient(new Uri(kvUrl), new DefaultAzureCredential());
builder.Services.AddSingleton(secretClient);

// Resolve secrets at startup using two-step lookup
var clientIdKey = Environment.GetEnvironmentVariable("ZOHO-CLIENT-ID") ?? throw new InvalidOperationException("Env var 'ZOHO-CLIENT-ID' is not set");
var clientSecretKey = Environment.GetEnvironmentVariable("ZOHO-CLIENT-SECRET") ?? throw new InvalidOperationException("Env var 'ZOHO-CLIENT-SECRET' is not set");
var tokenUrlKey = Environment.GetEnvironmentVariable("ZOHO_TOKEN_URL") ?? throw new InvalidOperationException("Env var 'ZOHO_TOKEN_URL' is not set");
var refreshTokenKey = Environment.GetEnvironmentVariable("ZOHO-REFRESH-TOKEN") ?? throw new InvalidOperationException("Env var 'ZOHO-REFRESH-TOKEN' is not set");
var baseUrlKey = Environment.GetEnvironmentVariable("ZOHO_BASE_URL") ?? throw new InvalidOperationException("Env var 'ZOHO_BASE_URL' is not set");

var clientId = (await secretClient.GetSecretAsync(clientIdKey)).Value.Value ?? throw new InvalidOperationException("ClientId secret not found");
var clientSecret = (await secretClient.GetSecretAsync(clientSecretKey)).Value.Value ?? throw new InvalidOperationException("ClientSecret secret not found");
var tokenUrl = (await secretClient.GetSecretAsync(tokenUrlKey)).Value.Value ?? throw new InvalidOperationException("TokenUrl secret not found");
var refreshToken = (await secretClient.GetSecretAsync(refreshTokenKey)).Value.Value ?? throw new InvalidOperationException("RefreshToken secret not found");
var baseUrlRaw = (await secretClient.GetSecretAsync(baseUrlKey)).Value.Value ?? throw new InvalidOperationException("BaseUrl secret not found");

var baseUrl = baseUrlRaw.TrimEnd('/');

var zohoOptions = new ZohoOptions
{
    ClientId = clientId,
    ClientSecret = clientSecret,
    TokenUrl = tokenUrl,
    RefreshToken = refreshToken,
    BaseUrl = baseUrl,
    Scopes = "ZohoCRM.users.ALL"
};

builder.Services.AddSingleton(zohoOptions);

// HTTP clients
builder.Services.AddHttpClient("zoho-api", client =>
{
    client.BaseAddress = new Uri(zohoOptions.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("zoho-token", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// DI registrations
builder.Services.AddSingleton<IZohoCrmConnection, ZohoCrmConnection>();
builder.Services.AddScoped<IZohoCrmService, ZohoCrmService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapControllers();

app.Run();
