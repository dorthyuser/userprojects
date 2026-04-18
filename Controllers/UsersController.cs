using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ZohoCrmOauthFinal1.Services;
using ZohoCrmOauthFinal1.Models;

namespace ZohoCrmOauthFinal1.Controllers
{
    [ApiController]
    [Route("crm/v2/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var result = await _userService.GetUsersAsync();
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users from Zoho CRM");
                return StatusCode(502, new { error = "Failed to fetch users" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] object createPayload)
        {
            try
            {
                var result = await _userService.CreateUserAsync(createPayload);
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user in Zoho CRM");
                return StatusCode(502, new { error = "Failed to create user" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser([FromRoute] string id, [FromBody] object updatePayload)
        {
            try
            {
                var result = await _userService.UpdateUserAsync(id, updatePayload);
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user in Zoho CRM");
                return StatusCode(502, new { error = "Failed to update user" });
            }
        }
    }
}