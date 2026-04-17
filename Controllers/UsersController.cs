using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZohoCrmOauthFinal.Services;

namespace ZohoCrmOauthFinal.Controllers
{
    [ApiController]
    [Route("crm/v2/users")]
    public class UsersController : ControllerBase
    {
        private readonly IZohoUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IZohoUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var result = await _userService.GetUsersAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve users from Zoho CRM");
                return StatusCode(500, new { error = "Failed to retrieve users" });
            }
        }
    }
}
