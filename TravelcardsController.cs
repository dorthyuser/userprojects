using azuretravelcardapi1218.Models;
using azuretravelcardapi1218.Services;
using Microsoft.AspNetCore.Mvc;

namespace azuretravelcardapi1218.Controllers;

[ApiController]
[Route("")]
public class TravelcardsController : ControllerBase
{
    private readonly ITravelcardService _travelcardService;
    private readonly ILogger<TravelcardsController> _logger;

    public TravelcardsController(ITravelcardService travelcardService, ILogger<TravelcardsController> logger)
    {
        _travelcardService = travelcardService;
        _logger = logger;
    }

    [HttpPost("travelcards")]
    public async Task<IActionResult> CreateTravelcard([FromHeader(Name = "client_id")] string? clientId, [FromHeader(Name = "X-Correlation-Cust-Id")] string? correlationId, [FromBody] TravelcardCreateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId) || clientId.Length < 1 || clientId.Length > 128)
        {
            return BadRequest(new { error = "client_id required, min 1 max 128 chars" });
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

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
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred." });
        }
    }
}