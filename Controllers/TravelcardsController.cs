using azuretravelcardapi121.Models;
using azuretravelcardapi121.Services;
using Microsoft.AspNetCore.Mvc;

namespace azuretravelcardapi121.Controllers;

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
    public async Task<IActionResult> CreateTravelcard([FromHeader(Name = "client_id")] string? clientId, [FromHeader(Name = "X-Correlation-Cust-Id")] string? correlationId, [FromBody] TravelcardCreateRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("POST /travelcards called. client_id={ClientId}, correlation_id={CorrelationId}", clientId, correlationId);

        if (string.IsNullOrWhiteSpace(clientId) || clientId.Length < 1 || clientId.Length > 128)
        {
            return BadRequest(new { error = "client_id required, max 128 chars" });
        }

        try
        {
            var response = await _service.CreateAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validation failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in POST /travelcards");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred." });
        }
    }
}