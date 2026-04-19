using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ZohoProject1.Models;
using ZohoProject1.Services;

var builder = WebApplication.CreateBuilder(args);

// Optional config file
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// Read values directly from environment variables
var clientId = Environment.GetEnvironmentVariable("ZOHO_CLIENT_ID")
    ?? throw new InvalidOperationException("ZOHO_CLIENT_ID not set");

var clientSecret = Environment.GetEnvironmentVariable("ZOHO_CLIENT_SECRET")
    ?? throw new InvalidOperationException("ZOHO_CLIENT_SECRET not set");

var tokenUrl = Environment.GetEnvironmentVariable("ZOHO_TOKEN_URL")
    ?? throw new InvalidOperationException("ZOHO_TOKEN_URL not set");

var refreshToken = Environment.GetEnvironmentVariable("ZOHO_REFRESH_TOKEN")
    ?? throw new InvalidOperationException("ZOHO_REFRESH_TOKEN not set");

var baseUrlRaw = Environment.GetEnvironmentVariable("ZOHO_BASE_URL")
    ?? throw new InvalidOperationException("ZOHO_BASE_URL not set");

// Normalize base URL
var baseUrl = baseUrlRaw.TrimEnd('/');

// Bind options
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
