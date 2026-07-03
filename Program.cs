using System.Text.Json.Serialization;
using currency_calculator.Infrastructure;
using currency_calculator.Repositories;
using currency_calculator.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton(sp =>
{
    var host = SecretHelper.Get("POSTGRESQL_HOST", "POSTGRESQL_HOST");
    var port = SecretHelper.Get("POSTGRESQL_PORT", "POSTGRESQL_PORT");
    var database = SecretHelper.Get("POSTGRESQL_DATABASE", "POSTGRESQL_DATABASE");
    var username = SecretHelper.Get("POSTGRESQL_USERNAME", "POSTGRESQL_USERNAME");
    var password = SecretHelper.Get("POSTGRESQL_PASSWORD", "POSTGRESQL_PASSWORD");
    var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};Pooling=true;Maximum Pool Size=50;Timeout=15;Command Timeout=30";
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
    return dataSourceBuilder.Build();
});

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ICurrencyTransferService, CurrencyTransferService>();
builder.Services.AddScoped<ICurrencyTransferRepository, CurrencyTransferRepository>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

var app = builder.Build();

app.MapControllers();

app.Run();