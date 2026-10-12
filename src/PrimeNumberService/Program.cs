using PrimeNumberService.Configuration;
using PrimeNumberService.Middleware;
using PrimeNumberService.Services;
using PrimeNumberService.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ISecretProvider, AwsSecretsManagerSecretProvider>();
builder.Services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
builder.Services.AddScoped<IPrimeNumberService, PrimeNumberService.Services.PrimeNumberService>();
builder.Services.AddSingleton<PrimeNumberValidator>();
builder.Services.Configure<ServiceOptions>(builder.Configuration.GetSection(ServiceOptions.SectionName));

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.Configure(options => options.ActivityTrackingOptions = System.Diagnostics.ActivityTrackingOptions.TraceId | System.Diagnostics.ActivityTrackingOptions.SpanId);

var app = builder.Build();

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program { }