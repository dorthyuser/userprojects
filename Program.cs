using System.Text.Json.Serialization;
using new_project.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(null, allowIntegerValues: false)));
builder.Services.AddScoped<IDemosService, DemosService>();

var app = builder.Build();

app.MapControllers();

app.Run();