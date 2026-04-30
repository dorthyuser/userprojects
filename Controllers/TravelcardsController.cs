using azuresharpapi153.Models.Requests;
using azuresharpapi153.Models.Responses;
using azuresharpapi153.Services;
using Microsoft.AspNetCore.Mvc;

namespace azuresharpapi153.Controllers;

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
    public async Task<ActionResult<CreateTravelcardResponse>> CreateTravelcard([FromHeader(Name = "client_id")] string clientId, [FromHeader(Name = "X-Correlation-Cust-Id")] string? correlationId, [FromBody] CreateTravelcardRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return BadRequest(new { message = "client_id is required." });
        }

        try
        {
            var response = await _travelcardService.CreateAsync(request, correlationId, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation failed while creating travelcard.");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating travelcard.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
        }
    }
}