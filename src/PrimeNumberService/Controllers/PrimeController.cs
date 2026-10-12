using Microsoft.AspNetCore.Mvc;
using PrimeNumberService.Models;
using PrimeNumberService.Services;

namespace PrimeNumberService.Controllers;

[ApiController]
[Route("prime")]
public sealed class PrimeController : ControllerBase
{
    private readonly IPrimeNumberService _primeNumberService;
    private readonly ILogger<PrimeController> _logger;

    public PrimeController(IPrimeNumberService primeNumberService, ILogger<PrimeController> logger)
    {
        _primeNumberService = primeNumberService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PrimeNumberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PrimeNumberResponse>> Get(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Route entry: GET /prime");
        var primeNumber = await _primeNumberService.GetPrimeNumberBetween100And200Async(cancellationToken);
        return Ok(new PrimeNumberResponse { PrimeNumber = primeNumber });
    }
}