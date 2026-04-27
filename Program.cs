using Microsoft.Extensions.Options;
using test_sf_git_prop.Models;
using test_sf_git_prop.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:8080");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging();

builder.Services.Configure<SalesforceOptions>(options =>
{
    options.Host = Environment.GetEnvironmentVariable("SALESFORCE_HOST") ?? builder.Configuration["Salesforce:Host"] ?? string.Empty;
    options.Port = int.TryParse(Environment.GetEnvironmentVariable("SALESFORCE_PORT") ?? builder.Configuration["Salesforce:Port"], out var port) ? port : 8080;
    options.Username = Environment.GetEnvironmentVariable("SALESFORCE_USERNAME") ?? builder.Configuration["Salesforce:Username"] ?? string.Empty;
    options.Password = Environment.GetEnvironmentVariable("SALESFORCE_PASSWORD") ?? builder.Configuration["Salesforce:Password"] ?? string.Empty;
    options.SecurityToken = Environment.GetEnvironmentVariable("SALESFORCE_SECURITY_TOKEN") ?? builder.Configuration["Salesforce:SecurityToken"] ?? string.Empty;
    options.ClientId = Environment.GetEnvironmentVariable("SALESFORCE_CLIENT_ID") ?? builder.Configuration["Salesforce:ClientId"] ?? string.Empty;
    options.ClientSecret = Environment.GetEnvironmentVariable("SALESFORCE_CLIENT_SECRET") ?? builder.Configuration["Salesforce:ClientSecret"] ?? string.Empty;
    options.ApiVersion = Environment.GetEnvironmentVariable("SALESFORCE_API_VERSION") ?? builder.Configuration["Salesforce:ApiVersion"] ?? "v59.0";
    options.InstanceUrl = Environment.GetEnvironmentVariable("SALESFORCE_INSTANCE_URL") ?? builder.Configuration["Salesforce:InstanceUrl"] ?? string.Empty;
    options.RedirectUri = Environment.GetEnvironmentVariable("SALESFORCE_REDIRECT_URI") ?? builder.Configuration["Salesforce:RedirectUri"] ?? string.Empty;
    options.UseSandbox = bool.TryParse(builder.Configuration["Salesforce:UseSandbox"], out var useSandbox) ? useSandbox : true;
    options.ProductionLoginUrl = builder.Configuration["Salesforce:ProductionLoginUrl"] ?? "https://login.salesforce.com";
    options.SandboxLoginUrl = builder.Configuration["Salesforce:SandboxLoginUrl"] ?? "https://test.salesforce.com";
});

builder.Services.AddHttpClient<ISalesforceClient, SalesforceClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<SalesforceOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(100);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    if (!string.IsNullOrWhiteSpace(options.InstanceUrl))
    {
        client.BaseAddress = new Uri(options.InstanceUrl);
    }
});

builder.Services.AddScoped<IAccountService, AccountService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();