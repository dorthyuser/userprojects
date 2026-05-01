using Microsoft.AspNetCore.Mvc;
using demo_travelcard_paul.Models;
using demo_travelcard_paul.Services;

namespace demo_travelcard_paul.Controllers;

[ApiController]
[Route("travelcards")]
public class TravelcardsController : ControllerBase
{
    private readonly ITravelcardsService _service;
    private readonly ILogger<TravelcardsController> _logger;

    public TravelcardsController(ITravelcardsService service, ILogger<TravelcardsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromHeader(Name = "client_id")] string? clientId, [FromBody] CreateTravelcardRequest request, [FromHeader(Name = "X-Correlation-Cust-Id")] string? correlationId = null)
    {
        _logger.LogInformation("POST /resource called. client_id={ClientId}", clientId);
        if (string.IsNullOrWhiteSpace(clientId) || clientId.Length < 1 || clientId.Length > 128 || !System.Text.RegularExpressions.Regex.IsMatch(clientId, "^[\\w+]+$"))
        {
            return BadRequest(new { error = "client_id required, 1-128 chars, pattern ^[\\w+]+$" });
        }

        if (!Request.Headers.TryGetValue("Content-Type", out var contentType) || !contentType.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Content-Type must contain application/json" });
        }

        if (!string.IsNullOrWhiteSpace(correlationId) && (correlationId.Length > 100 || !System.Text.RegularExpressions.Regex.IsMatch(correlationId, "^[A-Za-z0-9_-]+$")))
        {
            return BadRequest(new { error = "X-Correlation-Cust-Id invalid" });
        }

        try
        {
            var result = await _service.CreateAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validation failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in POST /resource");
            return StatusCode(500, new { error = "Unexpected error" });
        }
    }
}