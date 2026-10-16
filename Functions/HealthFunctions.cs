using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Azfcsharp8Function;

public sealed class HealthFunctions
{
    [Function("Alive")]
    public IActionResult Alive([HttpTrigger(AuthorizationLevel.Function, "get", "post", "put", "delete", "patch", "options", "head", Route = "alive")] HttpRequest req)
        => new ContentResult { StatusCode = 200, Content = "UP", ContentType = "text/plain" };

    [Function("Ready")]
    public IActionResult Ready([HttpTrigger(AuthorizationLevel.Function, "get", "post", "put", "delete", "patch", "options", "head", Route = "ready")] HttpRequest req)
        => new ContentResult { StatusCode = 200, Content = "UP", ContentType = "text/plain" };
}
