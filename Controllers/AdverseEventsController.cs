using csharpapi248pm.Models;
using csharpapi248pm.Services;
using Microsoft.AspNetCore.Mvc;

namespace csharpapi248pm.Controllers;

[ApiController]
[Route("v1/adverse-events")]
public sealed class AdverseEventsController : ControllerBase
{
    private readonly IAdverseEventService _service;
    private readonly ILogger<AdverseEventsController> _logger;

    public AdverseEventsController(IAdverseEventService service, ILogger<AdverseEventsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<AdverseEventSubmitResponse>> Submit([FromBody] AdverseEventRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP {Method} {Route}", HttpContext.Request.Method, HttpContext.Request.Path);
        var response = await _service.SubmitAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("notifications")]
    public async Task<ActionResult<AdverseEventNotificationsResponse>> GetNotifications([FromQuery] AdverseEventNotificationsQueryRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP {Method} {Route}", HttpContext.Request.Method, HttpContext.Request.Path);
        var response = await _service.GetNotificationsAsync(request, cancellationToken);
        return Ok(response);
    }
}