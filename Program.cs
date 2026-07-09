using currency_calculator.Infrastructure;
using currency_calculator.Repositories;
using currency_calculator.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<SecretHelper>();
builder.Services.AddSingleton<ICurrencyTransferRepository, CurrencyTransferRepository>();
builder.Services.AddSingleton<ICurrencyTransferService, CurrencyTransferService>();

var app = builder.Build();

app.MapControllers();

app.Run();