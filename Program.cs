using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TcTestingZoho.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration and logging
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();

// HTTP clients
builder.Services.AddHttpClient("OAuthClient");
builder.Services.AddHttpClient("TravelcardClient");

// Services
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<ITravelcardService, TravelcardService>();

builder.Services.AddEndpointsApiExplorer();

// Configure Kestrel to use port from configuration or default 8080
builder.WebHost.ConfigureKestrel(options =>
{
    // If port is configured in config, use it; otherwise default to 8080
    var portString = builder.Configuration["Backend:Port"];
    if (!int.TryParse(portString, out var port))
    {
        port = 8080;
    }

    options.ListenAnyIP(port);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.Run();
