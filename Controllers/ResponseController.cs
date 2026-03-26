using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ResponseHttp.Services;
using ResponseHttp.Models;

namespace ResponseHttp
{
    /// <summary>
    /// Controller that exposes an endpoint to fetch a response from the configured HTTPS connection.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class ResponseController : ControllerBase
    {
        private readonly IResponseService _responseService;
        private readonly ILogger<ResponseController> _logger;

        /// <summary>
        /// Controller constructor with DI injected response service.
        /// </summary>
        public ResponseController(IResponseService responseService, ILogger<ResponseController> logger)
        {
            _responseService = responseService;
            _logger = logger;
        }

        /// <summary>
        /// GET /response
        /// Calls the response service which performs an HTTPS request using the configured http connection and returns the concatenated result.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var result = await _responseService.FetchAndFormatResponseAsync();
                return Ok(new FetchResponse { Data = result });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error while fetching response");
                return StatusCode(500, "An error occurred while fetching the response.");
            }
        }
    }
}
