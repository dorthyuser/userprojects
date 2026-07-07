using System.Globalization;
using currency_calculator.Infrastructure;
using currency_calculator.Repositories;
using currency_calculator.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<SecretHelper>();
builder.Services.AddSingleton<ICurrencyTransferRepository, CurrencyTransferRepository>();
builder.Services.AddSingleton<ICurrencyTransferService, CurrencyTransferService>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddLogging();

var app = builder.Build();

app.UseRouting();
app.MapControllers();

app.Run();
