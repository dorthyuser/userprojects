using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ZohoProject3.Models;
using ZohoProject3.Services;

namespace ZohoProject3.Controllers
{
    [ApiController]
    [Route("crm/v2/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IZohoCrmService _service;

        public UsersController(IZohoCrmService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            try
            {
                var users = await _service.GetUsersAsync(cancellationToken);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest req, CancellationToken cancellationToken)
        {
            try
            {
                var created = await _service.CreateUserAsync(req, cancellationToken);
                return Ok(created);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequest req, CancellationToken cancellationToken)
        {
            try
            {
                var updated = await _service.UpdateUserAsync(id, req, cancellationToken);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
