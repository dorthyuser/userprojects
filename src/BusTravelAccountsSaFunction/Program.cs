using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BusTravelAccountsSaFunction;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Every HTTP invocation: a correlation id and one log line (outer); an exception a function lets escape: logged and
// answered with the Mule app's error body for an unexpected error (inner)
builder.UseMiddleware<RequestLoggingMiddleware>();
builder.UseMiddleware<ExceptionHandlingMiddleware>();

// Logs and traces through OpenTelemetry (host.json: telemetryMode), to Application Insights when the function app has
// its connection string. The worker's logs don't pass through the host: they carry their scopes (the correlation id)
// to the exporter, and on a developer's machine they're also written to the console (the host shows the worker's
// console; in Azure it would only duplicate the exported logs)
var telemetry = builder.Services.AddOpenTelemetry().UseFunctionsWorkerDefaults();
if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    telemetry.UseAzureMonitorExporter();
}
builder.Logging.AddOpenTelemetry(options => options.IncludeScopes = true);
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    });
}
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

// The application's services, clients, repositories and settings (Configuration/ServiceCollectionExtensions.cs)
builder.Services.AddApplicationServices(builder.Configuration);

builder.Build().Run();
