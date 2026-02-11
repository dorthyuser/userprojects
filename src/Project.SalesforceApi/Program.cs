using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Project.SalesforceApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Register a placeholder implementation; in real use swap with a Salesforce integration implementation
builder.Services.AddSingleton<ISalesforceService, SalesforceService>();

var app = builder.Build();
app.MapControllers();

app.Run();
