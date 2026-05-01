using System.Text.Json;
using System.Text.Json.Serialization;
using azuretravelcardapi121.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<ITravelcardsService, TravelcardsService>();

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();
app.Run();