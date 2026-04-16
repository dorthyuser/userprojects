using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TcTestingZoho.Services;

namespace TcTestingZoho.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TravelcardController : ControllerBase
    {
        private readonly ITravelcardService _travelcardService;
        private readonly ILogger<TravelcardController> _logger;

        public TravelcardController(ITravelcardService travelcardService, ILogger<TravelcardController> logger)
        {
            _travelcardService = travelcardService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            string body;
            using (var reader = new StreamReader(Request.Body))
            {
                body = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrEmpty(body))
            {
                return BadRequest(new { error = "Request body is required." });
            }

            try
            {
                var result = await _travelcardService.ForwardAsync(body);

                if (result.IsSuccess)
                {
                    // Preserve response status and content
                    return StatusCode(result.StatusCode, result.Content ?? string.Empty);
                }

                // For failures we preserve status code if available, otherwise return 502
                var status = result.StatusCode >= 400 ? result.StatusCode : 502;
                return StatusCode(status, new { error = result.Error ?? "Upstream request failed." });
            }
            catch (TokenException tex)
            {
                _logger.LogError(tex, "Token generation failed.");
                return StatusCode(502, new { error = "Token generation failed.", details = tex.Message });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while forwarding travelcard request.");
                return StatusCode(500, new { error = "Internal server error." });
            }
        }
    }
}
