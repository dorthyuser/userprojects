using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZohoProject1.Models;
using ZohoProject1.Services;

namespace ZohoProject1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IZohoCrmService _service;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IZohoCrmService service, ILogger<UsersController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            try
            {
                var result = await _service.GetUsersAsync(cancellationToken);
                return Content(result, "application/json");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Get users failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] User user, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _service.CreateUserAsync(user, cancellationToken);
                return Content(result, "application/json");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Create user failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] User user, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _service.UpdateUserAsync(id, user, cancellationToken);
                return Content(result, "application/json");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Update user failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
