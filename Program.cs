using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using csharpapi248pm.Infrastructure;
using csharpapi248pm.Repositories;
using csharpapi248pm.Services;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddControllers();

var host = SecretHelper.Get("POSTGRESQLHOST", "POSTGRESQLHOST");
var port = SecretHelper.Get("POSTGRESQLPORT", "POSTGRESQLPORT");
var database = SecretHelper.Get("POSTGRESQLDATABASE", "POSTGRESQLDATABASE");
var username = SecretHelper.Get("POSTGRESQLUSERNAME", "POSTGRESQLUSERNAME");
var password = SecretHelper.Get("POSTGRESQLPASSWORD", "POSTGRESQLPASSWORD");

var dataSourceBuilder = new NpgsqlDataSourceBuilder($"Host={host};Port={port};Database={database};Username={username};Password={password}");
var dataSource = dataSourceBuilder.Build();

builder.Services.AddSingleton(dataSource);
builder.Services.AddScoped<IAdverseEventRepository, AdverseEventRepository>();
builder.Services.AddScoped<IAdverseEventService, AdverseEventService>();

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();
app.Run();