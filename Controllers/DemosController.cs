using Microsoft.AspNetCore.Mvc;
using new_project.Models;
using new_project.Services;

namespace new_project.Controllers;

[ApiController]
[Route("demos")]
public sealed class DemosController : ControllerBase
{
    private readonly IDemosService _demosService;
    private readonly ILogger<DemosController> _logger;

    public DemosController(IDemosService demosService, ILogger<DemosController> logger)
    {
        _demosService = demosService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DemoResponse>>> Get(CancellationToken cancellationToken)
    {
        var clientId = Request.Headers["client_id"].ToString();
        _logger.LogInformation("{Method} /{Route} called. client_id={ClientId}", "GET", "demos", clientId);
        if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
        {
            return BadRequest(new { error = "client_id required, max 128 chars" });
        }

        try
        {
            var result = await _demosService.GetAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Route}", "demos");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred." });
        }
    }
}