using HotelBookingService.Models;
using HotelBookingService.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingService.Controllers;

[ApiController]
[Route("v1/hotels")]
public sealed class HotelsController : ControllerBase
{
    private readonly IHotelAvailabilityService _hotelAvailabilityService;
    private readonly ILogger<HotelsController> _logger;

    public HotelsController(IHotelAvailabilityService hotelAvailabilityService, ILogger<HotelsController> logger)
    {
        _hotelAvailabilityService = hotelAvailabilityService;
        _logger = logger;
    }

    [HttpGet("availability")]
    public async Task<ActionResult<HotelAvailabilityResponse>> GetAvailability([FromQuery] HotelAvailabilityRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP {Method} {Route}", HttpContext.Request.Method, HttpContext.Request.Path);
        var result = await _hotelAvailabilityService.GetAvailabilityAsync(request, cancellationToken);
        return Ok(result);
    }
}
