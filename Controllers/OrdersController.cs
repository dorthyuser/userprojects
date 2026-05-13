using Microsoft.AspNetCore.Mvc;
using test_capi_1111123323333.Models;
using test_capi_1111123323333.Services;

namespace test_capi_1111123323333.Controllers;

[ApiController]
[Route("orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrdersService _ordersService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrdersService ordersService, ILogger<OrdersController> logger)
    {
        _ordersService = ordersService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetAll([FromHeader(Name = "client_id")] string? clientId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
        {
            return BadRequest(new { error = "client_id required, max 128 chars" });
        }

        _logger.LogInformation("{Method} /{Route} called. client_id={ClientId}", "GET", "orders", clientId);
        try
        {
            var result = await _ordersService.GetAllAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Route}", "/orders");
            throw;
        }
    }

    [HttpGet("{orderId:guid}")]
    public async Task<ActionResult<OrderResponse>> GetById([FromRoute] Guid orderId, [FromHeader(Name = "client_id")] string? clientId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
        {
            return BadRequest(new { error = "client_id required, max 128 chars" });
        }

        _logger.LogInformation("{Method} /{Route} called. client_id={ClientId}", "GET", "orders/{orderId}", clientId);
        try
        {
            var result = await _ordersService.GetByIdAsync(orderId, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Route}", "/orders/{orderId}");
            throw;
        }
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create([FromBody] CreateOrderRequest request, [FromHeader(Name = "client_id")] string? clientId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
        {
            return BadRequest(new { error = "client_id required, max 128 chars" });
        }

        _logger.LogInformation("{Method} /{Route} called. client_id={ClientId}", "POST", "orders", clientId);
        try
        {
            var result = await _ordersService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { orderId = result.OrderId }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Route}", "/orders");
            throw;
        }
    }

    [HttpPut("{orderId:guid}")]
    public async Task<ActionResult<OrderResponse>> Update([FromRoute] Guid orderId, [FromBody] UpdateOrderRequest request, [FromHeader(Name = "client_id")] string? clientId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
        {
            return BadRequest(new { error = "client_id required, max 128 chars" });
        }

        _logger.LogInformation("{Method} /{Route} called. client_id={ClientId}", "PUT", "orders/{orderId}", clientId);
        try
        {
            var result = await _ordersService.UpdateAsync(orderId, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Route}", "/orders/{orderId}");
            throw;
        }
    }

    [HttpDelete("{orderId:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid orderId, [FromHeader(Name = "client_id")] string? clientId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
        {
            return BadRequest(new { error = "client_id required, max 128 chars" });
        }

        _logger.LogInformation("{Method} /{Route} called. client_id={ClientId}", "DELETE", "orders/{orderId}", clientId);
        try
        {
            var deleted = await _ordersService.DeleteAsync(orderId, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Route}", "/orders/{orderId}");
            throw;
        }
    }
}