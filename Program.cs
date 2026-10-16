using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using Azfcsharp8Function;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.UseMiddleware<RequestLogMiddleware>();

builder.Build().Run();
