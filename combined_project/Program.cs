using combined_project;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/", () => Results.Ok(new { service = "combined_project", status = "ok" }));
app.Run();
