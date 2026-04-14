using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace tc_csharp_api
{
    [ApiController]
    [Route("[controller]")]
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

            if (string.IsNullOrWhiteSpace(body))
            {
                return BadRequest(new { error = "Request body is empty" });
            }

            try
            {
                var response = await _travelcardService.ForwardAsync(body);
                var responseContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, responseContent);
            }
            catch (TokenException ex)
            {
                _logger.LogError(ex, "Token generation failed");
                return StatusCode(502, new { error = "Token generation failed", detail = ex.Message });
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(ex, "External API call failed");
                return StatusCode((int)ex.StatusCode, new { error = "External API call failed", detail = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error");
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }
    }
}
