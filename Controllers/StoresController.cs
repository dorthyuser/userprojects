using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using hello_http_test.Services;
using Microsoft.Extensions.Logging;

namespace hello_http_test.Controllers
{
    [ApiController]
    [Route("stores")]
    public class StoresController : ControllerBase
    {
        private readonly IZohoService _zohoService;
        private readonly ILogger<StoresController> _logger;

        public StoresController(IZohoService zohoService, ILogger<StoresController> logger)
        {
            _zohoService = zohoService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var stores = await _zohoService.GetStoresAsync();
                return Ok(stores);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error when fetching stores");
                return StatusCode(502, new { error = "Bad Gateway", detail = httpEx.Message });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error when fetching stores");
                return StatusCode(500, new { error = "Internal Server Error", detail = ex.Message });
            }
        }
    }
}
