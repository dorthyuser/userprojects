using System.Text.Json.Serialization;
using travelcardcsharpsb1114.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddScoped<ITravelcardService, TravelcardService>();
var app = builder.Build();
app.MapControllers();
app.Run();