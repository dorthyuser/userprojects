using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace BusTravelAccountsSaFunction;

public sealed class HealthFunctions(IAccountsRepository repository)
{
    [Function("Alive")]
    public IActionResult Alive(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", "put", "patch", "delete", Route = "alive")] HttpRequest req) =>
        new ContentResult { StatusCode = StatusCodes.Status200OK, Content = "UP", ContentType = "text/plain" };

    [Function("Ready")]
    public async Task<IActionResult> Ready(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", "put", "patch", "delete", Route = "ready")] HttpRequest req)
    {
        await repository.CheckHealthAsync(req.HttpContext.RequestAborted);
        return new ContentResult { StatusCode = StatusCodes.Status200OK, Content = "UP", ContentType = "text/plain" };
    }
}