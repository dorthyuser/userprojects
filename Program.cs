using System.Text.Json.Serialization;
using testing2.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(null, allowIntegerValues: false)));
builder.Services.AddScoped<ITestsService, TestsService>();

var app = builder.Build();

app.MapControllers();

app.Run();