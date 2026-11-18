using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BusTravelAccountsSaFunction;

public sealed class ServiceResult
{
    private ServiceResult(IActionResult actionResult)
    {
        ActionResult = actionResult;
    }

    public IActionResult ActionResult { get; }

    public static ServiceResult Ok(object payload) => new(new OkObjectResult(payload));

    public static ServiceResult NoContentArray() => new(new StatusCodeResult(StatusCodes.Status204NoContent));

    public IActionResult ToActionResult() => ActionResult;
}
