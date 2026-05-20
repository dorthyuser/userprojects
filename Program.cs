using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using zohotesting.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = null;
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(null, allowIntegerValues: false));
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddSingleton<IZohoHttpConnectionConnection, ZohoHttpConnectionConnection>();
builder.Services.AddSingleton<IZohoHttpConnectionService, ZohoHttpConnectionService>();

var host = SecretHelper.Get("POSTGRESQLHOST", "POSTGRESQLHOST");
var port = SecretHelper.Get("POSTGRESQLPORT", "POSTGRESQLPORT");
var database = SecretHelper.Get("POSTGRESQLDATABASE", "POSTGRESQLDATABASE");
var username = SecretHelper.Get("POSTGRESQLUSERNAME", "POSTGRESQLUSERNAME");
var password = SecretHelper.Get("POSTGRESQLPASSWORD", "POSTGRESQLPASSWORD");

var connStr = $"Host={host};Port={port};Database={database};Username={username};Password={password};";
var dsBuilder = new NpgsqlDataSourceBuilder(connStr);
var dataSource = dsBuilder.Build();

builder.Services.AddSingleton(dataSource);

var app = builder.Build();
app.MapControllers();
app.Run();