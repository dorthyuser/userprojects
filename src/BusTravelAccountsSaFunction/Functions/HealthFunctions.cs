using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace BusTravelAccountsSaFunction;

public sealed class HealthFunctions(IHealthService service)
{
    [Function("Alive")]
    public async Task<IActionResult> Alive(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", "put", "patch", "delete", Route = "alive")] HttpRequest req)
    {
        var result = await service.AliveAsync(req.HttpContext.RequestAborted);
        return new ContentResult { StatusCode = 200, Content = result, ContentType = "text/plain" };
    }

    [Function("Ready")]
    public async Task<IActionResult> Ready(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", "put", "patch", "delete", Route = "ready")] HttpRequest req)
    {
        var result = await service.ReadyAsync(req.HttpContext.RequestAborted);
        return new ContentResult { StatusCode = 200, Content = result, ContentType = "text/plain" };
    }
}
