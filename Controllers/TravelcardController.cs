using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using tc_testing_api2.Services;

namespace tc_testing_api2.Controllers
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
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                body = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrEmpty(body))
            {
                return BadRequest(new { error = "Empty request body" });
            }

            try
            {
                var response = await _travelcardService.ForwardAsync(body);
                var content = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, content);
            }
            catch (TokenException tex)
            {
                _logger.LogError(tex, "Token generation failed");
                return StatusCode((int)HttpStatusCode.BadGateway, new { error = "Token generation failed", detail = tex.Message });
            }
            catch (HttpRequestException hex)
            {
                _logger.LogError(hex, "External API request failed");
                return StatusCode((int)HttpStatusCode.BadGateway, new { error = "External API request failed", detail = hex.Message });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Unhandled error");
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }
    }
}