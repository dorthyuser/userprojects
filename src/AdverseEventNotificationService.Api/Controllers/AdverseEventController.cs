using AdverseEventNotificationService.Api.Contracts;
using AdverseEventNotificationService.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdverseEventNotificationService.Api.Controllers;

[ApiController]
[Route("v1/adverse-events")]
public sealed class AdverseEventController : ControllerBase
{
    private readonly IAdverseEventService _service;
    private readonly ILogger<AdverseEventController> _logger;

    public AdverseEventController(IAdverseEventService service, ILogger<AdverseEventController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AdverseEventResponseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AdverseEventResponseDto>> Submit([FromBody] AdverseEventRequestDto request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP POST /v1/adverse-events");
        var response = await _service.SubmitAsync(request, cancellationToken);
        return Created(string.Empty, response);
    }

    [HttpGet("notifications")]
    [ProducesResponseType(typeof(AdverseEventNotificationResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdverseEventNotificationResponseDto>> Notifications([FromQuery] NotificationQueryDto query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP GET /v1/adverse-events/notifications");
        var response = await _service.GetNotificationsAsync(query, cancellationToken);
        return Ok(response);
    }
}