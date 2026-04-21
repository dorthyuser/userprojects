using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using zoho_project_csharp.Models;
using zoho_project_csharp.Services;

namespace zoho_project_csharp.Controllers
{
    [ApiController]
    [Route("api")]
    public class UsersController : ControllerBase
    {
        private readonly IZohoCrmService _service;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IZohoCrmService service, ILogger<UsersController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
        {
            try
            {
                var (status, content) = await _service.GetUsersAsync(cancellationToken);
                return new ContentResult { StatusCode = status, Content = content, ContentType = "application/json" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUsers");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FirstName) || string.IsNullOrEmpty(request.LastName) || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Role?.Id) || string.IsNullOrEmpty(request.Profile?.Id))
                    return BadRequest(new { error = "first_name, last_name, email, role.id and profile.id are required" });

                var (status, content) = await _service.CreateUserAsync(request, cancellationToken);
                return new ContentResult { StatusCode = status, Content = content, ContentType = "application/json" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateUser");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser([FromRoute] string id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    return BadRequest(new { error = "id is required" });

                if (request == null)
                    return BadRequest(new { error = "request body is required" });

                // role.id and profile.id may be optional for update, but if provided they must be non-empty
                if (request.Role != null && string.IsNullOrEmpty(request.Role.Id))
                    return BadRequest(new { error = "role.id, if provided, must be non-empty" });
                if (request.Profile != null && string.IsNullOrEmpty(request.Profile.Id))
                    return BadRequest(new { error = "profile.id, if provided, must be non-empty" });

                var (status, content) = await _service.UpdateUserAsync(id, request, cancellationToken);
                return new ContentResult { StatusCode = status, Content = content, ContentType = "application/json" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateUser");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
