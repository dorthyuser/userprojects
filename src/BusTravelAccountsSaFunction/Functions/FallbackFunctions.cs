using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace BusTravelAccountsSaFunction;

public sealed class FallbackFunctions
{
    [Function("AccountsMethodNotAllowed")]
    public IActionResult AccountsMethodNotAllowed(
        [HttpTrigger(AuthorizationLevel.Function, "put", "patch", "delete", Route = "accounts")] HttpRequest req) =>
        ErrorResults.MethodNotAllowed();

    [Function("AccountByIdMethodNotAllowed")]
    public IActionResult AccountByIdMethodNotAllowed(
        [HttpTrigger(AuthorizationLevel.Function, "post", "patch", "delete", Route = "accounts/{id}")] HttpRequest req) =>
        ErrorResults.MethodNotAllowed();

    [Function("ResourceNotFound")]
    public IActionResult ResourceNotFound(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", "put", "patch", "delete", Route = "{*path:regex(^(?!((accounts|accounts/[^/]+|alive|ready)/?$)).*$)}")] HttpRequest req) =>
        ErrorResults.ResourceNotFound();
}