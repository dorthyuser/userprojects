using Microsoft.AspNetCore.Mvc;
using demo_travelcard_aus.Models;
using demo_travelcard_aus.Services;

namespace demo_travelcard_aus.Controllers;

[ApiController]
[Route("travelcards")]
public class TravelcardsController : ControllerBase
{
    private readonly ITravelcardService _service;
    private readonly ILogger<TravelcardsController> _logger;

    public TravelcardsController(ITravelcardService service, ILogger<TravelcardsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTravelcard(
        [FromHeader(Name = "client_id")] string? clientId,
        [FromHeader(Name = "Content-Type")] string? contentType,
        [FromHeader(Name = "X-Correlation-Cust-Id")] string? correlationId,
        [FromBody] TravelcardCreateRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("POST /resource called. client_id={ClientId}", clientId);

        try
        {
            if (string.IsNullOrWhiteSpace(clientId) || clientId.Length < 1 || clientId.Length > 128 || !System.Text.RegularExpressions.Regex.IsMatch(clientId, @"^[\w+]+$"))
                return BadRequest(new { error = "client_id required, max 128 chars, pattern ^[\\w+]+$" });

            if (Request.ContentLength > 0 && (string.IsNullOrWhiteSpace(contentType) || !contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase)))
                return BadRequest(new { error = "Content-Type must contain application/json" });

            if (!string.IsNullOrWhiteSpace(correlationId) && (correlationId.Length > 100 || !System.Text.RegularExpressions.Regex.IsMatch(correlationId, @"^[A-Za-z0-9_-]+$")))
                return BadRequest(new { error = "X-Correlation-Cust-Id invalid" });

            var result = await _service.CreateAsync(request, cancellationToken);
            _logger.LogInformation("Request completed successfully. Id={Id}", result.TravelcardId);
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
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }
}