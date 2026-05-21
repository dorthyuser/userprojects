using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using synctesting1109.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(null, allowIntegerValues: false)));
builder.Services.AddHttpClient("zoho-api", client => { client.Timeout = TimeSpan.FromSeconds(30); });
builder.Services.AddHttpClient("zoho-token", client => { client.Timeout = TimeSpan.FromSeconds(30); });
builder.Services.AddSingleton<IZohoHttpConnectionConnection, ZohoHttpConnectionConnection>();
builder.Services.AddSingleton<IZohoHttpConnectionService, ZohoHttpConnectionService>();

var app = builder.Build();
app.MapControllers();
app.Run();