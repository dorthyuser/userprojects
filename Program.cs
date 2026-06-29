using System.Text.Json.Serialization;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using currency_calculator.Infrastructure;
using currency_calculator.Repositories;
using currency_calculator.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton(sp =>
{
    var host = SecretHelper.Get("MYSQL_HOST", "MYSQL_HOST");
    var port = SecretHelper.Get("MYSQL_PORT", "MYSQL_PORT");
    var user = SecretHelper.Get("MYSQL_USER", "MYSQL_USER");
    var password = SecretHelper.Get("MYSQL_PASSWORD", "MYSQL_PASSWORD");
    var database = SecretHelper.Get("MYSQL_DATABASE", "MYSQL_DATABASE");
    var cs = $"Server={host};Port={port};User ID={user};Password={password};Database={database};SslMode=Required;";
    return new MySqlDataSourceBuilder(cs).Build();
});
builder.Services.AddSingleton<ICurrencyTransferRepository, CurrencyTransferRepository>();
builder.Services.AddSingleton<ICurrencyTransferService, CurrencyTransferService>();

var app = builder.Build();

app.MapControllers();

app.Run();