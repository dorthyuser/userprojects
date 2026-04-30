using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using azurefunction318.Services;
using azurefunction318.Models;

namespace azurefunction318.Controllers
{
    [ApiController]
    public class DefaultController : ControllerBase
    {
        private readonly ITravelcardService _service;
        private readonly ILogger<DefaultController> _logger;

        public DefaultController(ITravelcardService service, ILogger<DefaultController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost("/default")]
        public async Task<IActionResult> Create([FromHeader(Name = "client_id")] string clientId, [FromHeader(Name = "X-Correlation-Cust-Id")] string correlationId, [FromBody] TravelcardRequest request)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return BadRequest(new { error = "Missing required header: client_id" });
            }

            try
            {
                var result = await _service.CreateAsync(request, clientId, correlationId);
                return Created(string.Empty, result);
            }
            catch (ValidationException vex)
            {
                _logger.LogWarning(vex, "Validation failed for travelcard creation");
                return BadRequest(new { error = vex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error while creating travelcard");
                return StatusCode(500, new { error = "An unexpected error occurred" });
            }
        }
    }
}
