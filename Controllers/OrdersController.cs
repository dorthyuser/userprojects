using System;
using System.Threading;
using System.Threading.Tasks;
using jkjkjkjkjkjkjkjkjkjkjkjkjkjjk.Models;
using jkjkjkjkjkjkjkjkjkjkjkjkjkjjk.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace jkjkjkjkjkjkjkjkjkjkjkjkjkjjk.Controllers
{
    [ApiController]
    [Route("orders")]
    public sealed class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [HttpGet("{orderId}")]
        public async Task<ActionResult<OrderResponse>> GetOrder([FromRoute] string orderId, [FromHeader(Name = "client_id")] string? clientId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
            {
                return BadRequest(new { error = "client_id required, max 128 chars" });
            }

            _logger.LogInformation("{Method} /{Route} called. client_id={ClientId}", "GET", "orders/{orderId}", clientId);

            try
            {
                var response = await _orderService.GetByIdAsync(orderId, cancellationToken);
                return Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<ActionResult<OrderResponse>> CreateOrder([FromBody] CreateOrderRequest request, [FromHeader(Name = "client_id")] string? clientId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
            {
                return BadRequest(new { error = "client_id required, max 128 chars" });
            }

            _logger.LogInformation("{Method} /{Route} called. client_id={ClientId}", "POST", "orders", clientId);
            var response = await _orderService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetOrder), new { orderId = response.OrderId }, response);
        }

        [HttpPut("{orderId}")]
        public async Task<ActionResult<OrderResponse>> UpdateOrder([FromRoute] string orderId, [FromBody] UpdateOrderRequest request, [FromHeader(Name = "client_id")] string? clientId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
            {
                return BadRequest(new { error = "client_id required, max 128 chars" });
            }

            _logger.LogInformation("{Method} /{Route} called. client_id={ClientId}", "PUT", "orders/{orderId}", clientId);
            var response = await _orderService.UpdateAsync(orderId, request, cancellationToken);
            return Ok(response);
        }

        [HttpPost("{orderId}/cancelations")]
        public async Task<ActionResult<CancelOrderResponse>> CancelOrder([FromRoute] string orderId, [FromHeader(Name = "client_id")] string? clientId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
            {
                return BadRequest(new { error = "client_id required, max 128 chars" });
            }

            _logger.LogInformation("{Method} /{Route} called. client_id={ClientId}", "POST", "orders/{orderId}/cancelations", clientId);
            var response = await _orderService.CancelAsync(orderId, cancellationToken);
            return Ok(response);
        }
    }
}