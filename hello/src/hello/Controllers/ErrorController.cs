using hello.Application;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace hello.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public sealed class ErrorController : ControllerBase
{
    [Route("/error")]
    public IActionResult Error()
    {
        var feature = HttpContext.Features.Get<IExceptionHandlerFeature>();
        return Problem(title: "Internal Error", statusCode: StatusCodes.Status500InternalServerError);
    }
}
