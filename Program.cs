using System.Text.Json.Serialization;
using httptestingapi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(null, allowIntegerValues: false)));
builder.Services.AddScoped<ITravelcardService, TravelcardService>();

var app = builder.Build();

app.MapControllers();

app.Run();
