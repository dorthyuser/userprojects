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

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton(sp => MySqlConnectionFactory.Create());
builder.Services.AddSingleton<ICurrencyTransferRepository, CurrencyTransferRepository>();
builder.Services.AddSingleton<ICurrencyTransferService, CurrencyTransferService>();

var app = builder.Build();

app.MapControllers();

app.Run();