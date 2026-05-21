using Microsoft.AspNetCore.Mvc;
using synctesting1109.Models;
using synctesting1109.Services;

namespace synctesting1109.Controllers;

[ApiController]
public sealed class UsersController : ControllerBase
{
    private readonly IZohoHttpConnectionService _service;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IZohoHttpConnectionService service, ILogger<UsersController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("api/v1/users")]
    public async Task<IActionResult> CreateUser([FromBody] ZohoCreateUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.CreateUserAsync(Request, request, cancellationToken);
            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in api/v1/users");
            return StatusCode(500, new { error = "INTERNAL_SERVER_ERROR" });
        }
    }

    [HttpGet("api/v1/users")]
    public async Task<IActionResult> GetUsers([FromQuery] ZohoGetUsersQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetZohoUsersAsync(Request, query, cancellationToken);
            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in api/v1/users");
            return StatusCode(500, new { error = "INTERNAL_SERVER_ERROR" });
        }
    }

    [HttpGet("api/v1/users/{zoho_id}")]
    public async Task<IActionResult> GetUserByZohoId([FromRoute(Name = "zoho_id")] string zohoId, [FromQuery] ZohoGetUsersQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetZohoUserByIdAsync(Request, zohoId, query, cancellationToken);
            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in api/v1/users/{ZohoId}", zohoId);
            return StatusCode(500, new { error = "INTERNAL_SERVER_ERROR" });
        }
    }

    [HttpPost("api/v1/users/sync")]
    public async Task<IActionResult> SyncUsers([FromBody] ZohoSyncUsersRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.SyncUsersAsync(Request, request, cancellationToken);
            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in api/v1/users/sync");
            return StatusCode(500, new { error = "INTERNAL_SERVER_ERROR" });
        }
    }

    [HttpGet("api/v1/users/local")]
    public async Task<IActionResult> GetLocalUsers([FromQuery] LocalUsersQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetLocalUsersAsync(Request, query, cancellationToken);
            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in api/v1/users/local");
            return StatusCode(500, new { error = "INTERNAL_SERVER_ERROR" });
        }
    }

    [HttpGet("api/v1/users/local/{user_pk}")]
    public async Task<IActionResult> GetLocalUserByPk([FromRoute(Name = "user_pk")] string userPk, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetLocalUserByPkAsync(Request, userPk, cancellationToken);
            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in api/v1/users/local/{UserPk}", userPk);
            return StatusCode(500, new { error = "INTERNAL_SERVER_ERROR" });
        }
    }

    [HttpGet("api/v1/users/local/zoho/{zoho_uid}")]
    public async Task<IActionResult> GetLocalUserByZohoUid([FromRoute(Name = "zoho_uid")] string zohoUid, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetLocalUserByZohoUidAsync(Request, zohoUid, cancellationToken);
            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in api/v1/users/local/zoho/{ZohoUid}", zohoUid);
            return StatusCode(500, new { error = "INTERNAL_SERVER_ERROR" });
        }
    }
}