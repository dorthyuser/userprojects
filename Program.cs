using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using azurefunction318.Services;
using azurefunction318.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Configuration: load environment variables as well
builder.Configuration.AddEnvironmentVariables();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<ITravelcardService, TravelcardService>();
builder.Services.AddScoped<ITravelcardRepository, TravelcardRepository>();

builder.Services.AddLogging();

var app = builder.Build();

app.MapControllers();

// Determine port from appsettings or default to 8080
var port = builder.Configuration.GetValue<int?>("Application:Port") ?? 8080;
var url = $"http://0.0.0.0:{port}";

app.Urls.Clear();
app.Urls.Add(url);

app.Run();
