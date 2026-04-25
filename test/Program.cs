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

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

var kvUrl = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT");
if (string.IsNullOrEmpty(kvUrl))
    throw new InvalidOperationException("Env var 'AZURE_KEY_VAULT' is not set");

var secretClient = new SecretClient(new Uri(kvUrl), new DefaultAzureCredential());

async Task<string> ResolveSecretAsync(string envVarName)
{
    var secretKeyName = Environment.GetEnvironmentVariable(envVarName);
    if (string.IsNullOrEmpty(secretKeyName))
        throw new InvalidOperationException($"Env var '{envVarName}' is not set");

    var secret = await secretClient.GetSecretAsync(secretKeyName).ConfigureAwait(false);
    return secret.Value.Value ?? throw new InvalidOperationException($"Secret '{secretKeyName}' returned empty value");
}

var baseUrl = await ResolveSecretAsync("ZOHO_BASE_URL").ConfigureAwait(false);
baseUrl = baseUrl.TrimEnd('/');

var clientId = await ResolveSecretAsync("ZOHO_CLIENT_ID").ConfigureAwait(false);
var clientSecret = await ResolveSecretAsync("ZOHO_CLIENT_SECRET").ConfigureAwait(false);
var tokenUrl = await ResolveSecretAsync("ZOHO_TOKEN_URL").ConfigureAwait(false);
var refreshToken = await ResolveSecretAsync("ZOHO_REFRESH_TOKEN").ConfigureAwait(false);

builder.Services.AddSingleton(secretClient);

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

builder.Services.AddHttpClient("zoho_api", client =>
{
    client.BaseAddress = new Uri(zohoOptions.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("zoho_token", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

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
