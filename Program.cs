using System.Text.Json.Serialization;
using test_capi_1111123323333.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(null, allowIntegerValues: false)));
builder.Services.AddScoped<IOrdersService, OrdersService>();

var app = builder.Build();

app.MapControllers();

app.Run();