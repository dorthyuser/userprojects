using Microsoft.AspNetCore.Mvc;
using travelcardcsharpsb1114.Models;
using travelcardcsharpsb1114.Services;

namespace travelcardcsharpsb1114.Controllers;

[ApiController]
[Route("travelcards")]
public sealed class TravelcardsController : ControllerBase
{
    private readonly ITravelcardService _travelcardService;
    private readonly ILogger<TravelcardsController> _logger;

    public TravelcardsController(ITravelcardService travelcardService, ILogger<TravelcardsController> logger)
    {
        _travelcardService = travelcardService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromHeader(Name = "client_id")] string? clientId, [FromHeader(Name = "X-Correlation-Cust-Id")] string? correlationId, [FromBody] CreateTravelcardRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("POST /{Route} called. client_id={ClientId}", "travelcards", clientId);

        if (string.IsNullOrWhiteSpace(clientId) || clientId.Length < 1 || clientId.Length > 128)
        {
            return BadRequest(new { error = "client_id required, max 128 chars" });
        }

        try
        {
            var response = await _travelcardService.CreateAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validation failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in POST /{Route}", "travelcards");
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }
}