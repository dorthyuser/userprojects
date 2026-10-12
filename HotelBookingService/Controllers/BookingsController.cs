using HotelBookingService.Models;
using HotelBookingService.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingService.Controllers;

[ApiController]
[Route("v1/bookings")]
public sealed class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(IBookingService bookingService, ILogger<BookingsController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<CreateBookingResponse>> CreateBooking([FromBody] CreateBookingRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP {Method} {Route}", HttpContext.Request.Method, HttpContext.Request.Path);
        var result = await _bookingService.CreateBookingAsync(request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{bookingId}")]
    public async Task<ActionResult<GetBookingResponse>> GetBooking([FromRoute] string bookingId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP {Method} {Route}", HttpContext.Request.Method, HttpContext.Request.Path);
        var result = await _bookingService.GetBookingAsync(bookingId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{bookingId}/cancel")]
    public async Task<ActionResult<CancelBookingResponse>> CancelBooking([FromRoute] string bookingId, [FromBody] CancelBookingRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP {Method} {Route}", HttpContext.Request.Method, HttpContext.Request.Path);
        var result = await _bookingService.CancelBookingAsync(bookingId, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
