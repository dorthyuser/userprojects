using Microsoft.AspNetCore.Mvc;
using azuretravelcardapi907.Models;
using azuretravelcardapi907.Services;

namespace azuretravelcardapi907.Controllers;

[ApiController]
[Route("travelcards")]
public class TravelcardsController : ControllerBase
{
    private readonly ITravelcardService _travelcardService;
    private readonly ILogger<TravelcardsController> _logger;

    public TravelcardsController(ITravelcardService travelcardService, ILogger<TravelcardsController> logger)
    {
        _travelcardService = travelcardService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<CreateTravelcardResponse>> CreateTravelcard([FromHeader(Name = "client_id")] string? clientId, [FromHeader(Name = "X-Correlation-Cust-Id")] string? correlationId, [FromBody] CreateTravelcardRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
            return BadRequest(new { error = "client_id required, max 128 chars" });

        try
        {
            var response = await _travelcardService.CreateAsync(request, correlationId, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation failed creating travelcard");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error creating travelcard");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred" });
        }
    }
}