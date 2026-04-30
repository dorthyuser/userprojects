using azuresharpapi153.Data;
using azuresharpapi153.Services;
using Microsoft.EntityFrameworkCore;
using azuresharpapi153.Models.Entities;
using Npgsql;
using Npgsql.NameTranslation;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:8080");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<ITravelcardService, TravelcardService>();

var host = Environment.GetEnvironmentVariable("POSTGRESQL_HOST");
var port = Environment.GetEnvironmentVariable("POSTGRESQL_PORT");
var database = Environment.GetEnvironmentVariable("POSTGRESQL_DATABASE");
var username = Environment.GetEnvironmentVariable("POSTGRESQL_USERNAME");
var password = Environment.GetEnvironmentVariable("POSTGRESQL_PASSWORD");

var connectionString = builder.Configuration.GetConnectionString("PostgreSql");

if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};Pooling=true;";
}

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

dataSourceBuilder.MapEnum<TravelcardTypeEnum>("travelcard_type_enum", nameTranslator: new NpgsqlNullNameTranslator());
dataSourceBuilder.MapEnum<CardholderTypeEnum>("cardholder_type_enum", nameTranslator: new NpgsqlNullNameTranslator());

var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource, npgsql =>
    {
        npgsql.EnableRetryOnFailure();
    }));

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
