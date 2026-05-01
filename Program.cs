using System.Text.Json;
using System.Text.Json.Serialization;
using demo_travelcard_paul.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
builder.Services.AddScoped<ITravelcardsService, TravelcardsService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.WebHost.UseUrls("http://0.0.0.0:8080");
var app = builder.Build();
app.MapControllers();
app.Run();