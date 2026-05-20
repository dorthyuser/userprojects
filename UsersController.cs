using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using zohotesting.Models;
using zohotesting.Services;

namespace zohotesting.Controllers;

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
    public async Task<IActionResult> CreateUser([FromBody] CreateZohoUserRequest request, CancellationToken cancellationToken)
    {
        var clientId = Request.Headers["X-Correlation-Id"].ToString();
        _logger.LogInformation("{Method} /{Route} called. client_id={ClientId}", "POST", "api/v1/users", clientId);
        try
        {
            var result = await _service.CreateUserAsync(request, clientId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(result.CorrelationId))
            {
                Response.Headers["X-Correlation-Id"] = result.CorrelationId;
            }

            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Route}", "api/v1/users");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("api/v1/users")]
    public async Task<IActionResult> GetZohoUsers([FromQuery] string? type, [FromQuery] int? page, [FromQuery] int? per_page, [FromHeader(Name = "If-Modified-Since")] string? ifModifiedSince, CancellationToken cancellationToken)
    {
        var clientId = Request.Headers["X-Correlation-Id"].ToString();
        _logger.LogInformation("{Method} /{Route} called. client_id={ClientId}", "GET", "api/v1/users", clientId);
        try
        {
            var result = await _service.GetZohoUsersAsync(null, type, page, per_page, ifModifiedSince, clientId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(result.CorrelationId))
            {
                Response.Headers["X-Correlation-Id"] = result.CorrelationId;
            }

            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Route}", "api/v1/users");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("api/v1/users/{zoho_id}")]
    public async Task<IActionResult> GetZohoUserById([FromRoute] string zoho_id, [FromHeader(Name = "If-Modified-Since")] string? ifModifiedSince, CancellationToken cancellationToken)
    {
        var clientId = Request.Headers["X-Correlation-Id"].ToString();
        _logger.LogInformation("{Method} /{Route} called. client_id={ClientId}", "GET", "api/v1/users/{zoho_id}", clientId);
        try
        {
            var result = await _service.GetZohoUsersAsync(zoho_id, null, null, null, ifModifiedSince, clientId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(result.CorrelationId))
            {
                Response.Headers["X-Correlation-Id"] = result.CorrelationId;
            }

            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Route}", "api/v1/users/{zoho_id}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("api/v1/users/sync")]
    public async Task<IActionResult> SyncUsers([FromBody] SyncZohoUsersRequest? request, CancellationToken cancellationToken)
    {
        var clientId = Request.Headers["X-Correlation-Id"].ToString();
        _logger.LogInformation("{Method} /{Route} called. client_id={ClientId}", "POST", "api/v1/users/sync", clientId);
        try
        {
            var result = await _service.SyncUsersAsync(request, clientId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(result.CorrelationId))
            {
                Response.Headers["X-Correlation-Id"] = result.CorrelationId;
            }

            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Route}", "api/v1/users/sync");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("api/v1/users/local")]
    public async Task<IActionResult> GetLocalUsers([FromQuery] LocalUsersQuery request, CancellationToken cancellationToken)
    {
        var clientId = Request.Headers["X-Correlation-Id"].ToString();
        _logger.LogInformation("{Method} /{Route} called. client_id={ClientId}", "GET", "api/v1/users/local", clientId);
        try
        {
            var result = await _service.GetLocalUsersAsync(request, clientId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(result.CorrelationId))
            {
                Response.Headers["X-Correlation-Id"] = result.CorrelationId;
            }

            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Route}", "api/v1/users/local");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("api/v1/users/local/{user_pk:int}")]
    public async Task<IActionResult> GetLocalUserByPk([FromRoute] int user_pk, CancellationToken cancellationToken)
    {
        var clientId = Request.Headers["X-Correlation-Id"].ToString();
        _logger.LogInformation("{Method} /{Route} called. client_id={ClientId}", "GET", "api/v1/users/local/{user_pk}", clientId);
        try
        {
            var result = await _service.GetLocalUserByPkAsync(user_pk, clientId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(result.CorrelationId))
            {
                Response.Headers["X-Correlation-Id"] = result.CorrelationId;
            }

            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Route}", "api/v1/users/local/{user_pk}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("api/v1/users/local/zoho/{zoho_uid}")]
    public async Task<IActionResult> GetLocalUserByZohoUid([FromRoute] string zoho_uid, CancellationToken cancellationToken)
    {
        var clientId = Request.Headers["X-Correlation-Id"].ToString();
        _logger.LogInformation("{Method} /{Route} called. client_id={ClientId}", "GET", "api/v1/users/local/zoho/{zoho_uid}", clientId);
        try
        {
            var result = await _service.GetLocalUserByZohoUidAsync(zoho_uid, clientId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(result.CorrelationId))
            {
                Response.Headers["X-Correlation-Id"] = result.CorrelationId;
            }

            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Route}", "api/v1/users/local/zoho/{zoho_uid}");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}