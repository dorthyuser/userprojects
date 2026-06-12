using azurefunctionaeproject.Helpers;
using azurefunctionaeproject.Services;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);
builder.Services.AddSingleton<DbHelper>();
builder.Services.AddSingleton<AdverseEventService>();
builder.Services.AddLogging();
builder.Build().Run();
