using 123-sadsa-1232.Models;
using 123-sadsa-1232.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:8080");

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.WriteIndented = false;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<PaymentApiOptions>(builder.Configuration.GetSection("PaymentApi"));
builder.Services.AddHttpClient("icici", (sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PaymentApiOptions>>().Value;
    client.BaseAddress = new Uri(options.HttpBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<IPaymentService, PaymentService>();

var app = builder.Build();

app.UseExceptionHandler("/error");
app.Use(async (context, next) =>
{
    if (!context.Request.Headers.TryGetValue("correlation-id", out var correlationId) || string.IsNullOrWhiteSpace(correlationId))
    {
        context.Request.Headers["correlation-id"] = Guid.NewGuid().ToString("N");
    }
    context.Response.Headers["correlation-id"] = context.Request.Headers["correlation-id"].ToString();
    await next();
});

app.MapGet("/error", () => Results.Problem("An unexpected error occurred."));
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();