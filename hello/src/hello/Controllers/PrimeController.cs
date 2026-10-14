using hello.Application;
using hello.Services;
using Microsoft.AspNetCore.Mvc;

namespace hello.Controllers;

[ApiController]
[Route("prime")]
public sealed class PrimeController : ControllerBase
{
    private readonly IPrimeService _primeService;
    private readonly ILogger<PrimeController> _logger;

    public PrimeController(IPrimeService primeService, ILogger<PrimeController> logger)
    {
        _primeService = primeService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PrimeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PrimeResponse>> Get(CancellationToken cancellationToken)
    {
        _logger.LogInformation("route_entry method=GET path=/prime resource=prime");
        var prime = await _primeService.GetPrimeNumberBetween100And200Async(cancellationToken);
        return Ok(new PrimeResponse { Prime = prime });
    }
}
