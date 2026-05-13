using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using httptestingapi.Models;
using httptestingapi.Services;

namespace httptestingapi.Controllers;

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
    public async Task<ActionResult<CreateTravelcardResponse>> Create([FromHeader(Name = "client_id")] string? clientId, [FromHeader(Name = "X-Correlation-Cust-Id")] string? correlationId, [FromBody] CreateTravelcardRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("POST /travelcards called. client_id={ClientId}", clientId);

        if (string.IsNullOrWhiteSpace(clientId) || clientId.Length < 1 || clientId.Length > 128)
        {
            return BadRequest(new { error = "client_id required, min 1 and max 128 characters" });
        }

        var response = await _travelcardService.CreateAsync(request, cancellationToken);
        return Ok(response);
    }
}
