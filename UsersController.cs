using Microsoft.AspNetCore.Mvc;
using synctesting1050.Models;
using synctesting1050.Services;

namespace synctesting1050.Controllers;

[ApiController]
[Route("api/v1/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IZohoHttpConnectionService _service;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IZohoHttpConnectionService service, ILogger<UsersController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, [FromHeader(Name = "X-Correlation-Id")] string? correlationId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("POST /api/v1/users called. client_id={ClientId}", correlationId ?? string.Empty);
        try
        {
            var result = await _service.CreateUserAsync(request, correlationId, cancellationToken);
            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in /api/v1/users");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{zoho_id}")]
    public async Task<IActionResult> GetUserByZohoId([FromRoute] string zoho_id, [FromHeader(Name = "X-Correlation-Id")] string? correlationId, [FromHeader(Name = "If-Modified-Since")] string? ifModifiedSince, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/v1/users/{ZohoId} called. client_id={ClientId}", zoho_id, correlationId ?? string.Empty);
        try
        {
            var result = await _service.GetZohoUserAsync(zoho_id, correlationId, ifModifiedSince, cancellationToken);
            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in /api/v1/users/{ZohoId}", zoho_id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ListUsers([FromQuery] ZohoUsersListQuery query, [FromHeader(Name = "X-Correlation-Id")] string? correlationId, [FromHeader(Name = "If-Modified-Since")] string? ifModifiedSince, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/v1/users called. client_id={ClientId}", correlationId ?? string.Empty);
        try
        {
            var result = await _service.ListZohoUsersAsync(query, correlationId, ifModifiedSince, cancellationToken);
            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in /api/v1/users");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncUsers([FromBody] SyncUsersRequest request, [FromHeader(Name = "X-Correlation-Id")] string? correlationId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("POST /api/v1/users/sync called. client_id={ClientId}", correlationId ?? string.Empty);
        try
        {
            var result = await _service.SyncUsersAsync(request, correlationId, cancellationToken);
            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in /api/v1/users/sync");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("local")]
    public async Task<IActionResult> GetLocalUsers([FromQuery] LocalUsersQuery query, [FromHeader(Name = "X-Correlation-Id")] string? correlationId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/v1/users/local called. client_id={ClientId}", correlationId ?? string.Empty);
        try
        {
            var result = await _service.GetLocalUsersAsync(query, correlationId, cancellationToken);
            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in /api/v1/users/local");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("local/{user_pk:int}")]
    public async Task<IActionResult> GetLocalUserByPk([FromRoute] long user_pk, [FromHeader(Name = "X-Correlation-Id")] string? correlationId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/v1/users/local/{UserPk} called. client_id={ClientId}", user_pk, correlationId ?? string.Empty);
        try
        {
            var result = await _service.GetLocalUserByPkAsync(user_pk, correlationId, cancellationToken);
            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in /api/v1/users/local/{UserPk}", user_pk);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("local/zoho/{zoho_uid}")]
    public async Task<IActionResult> GetLocalUserByZohoUid([FromRoute] string zoho_uid, [FromHeader(Name = "X-Correlation-Id")] string? correlationId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/v1/users/local/zoho/{ZohoUid} called. client_id={ClientId}", zoho_uid, correlationId ?? string.Empty);
        try
        {
            var result = await _service.GetLocalUserByZohoUidAsync(zoho_uid, correlationId, cancellationToken);
            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in /api/v1/users/local/zoho/{ZohoUid}", zoho_uid);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}