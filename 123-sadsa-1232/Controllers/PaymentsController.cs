using Microsoft.AspNetCore.Mvc;
using _123_sadsa_1232.Models;
using _123_sadsa_1232.Services;

namespace _123_sadsa_1232.Controllers;

[ApiController]
[Route("payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetAll(CancellationToken cancellationToken)
    {
        var correlationId = GetOrCreateCorrelationId();
        Response.Headers["correlation-id"] = correlationId;
        var result = await _paymentService.GetAllAsync(correlationId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<PaymentDto>> GetById(long id, CancellationToken cancellationToken)
    {
        var correlationId = GetOrCreateCorrelationId();
        Response.Headers["correlation-id"] = correlationId;
        var result = await _paymentService.GetByIdAsync(id, correlationId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Create([FromBody] PaymentUpsertRequest request, CancellationToken cancellationToken)
    {
        var correlationId = GetOrCreateCorrelationId();
        Response.Headers["correlation-id"] = correlationId;
        var result = await _paymentService.CreateAsync(request, correlationId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<PaymentDto>> Update(long id, [FromBody] PaymentUpsertRequest request, CancellationToken cancellationToken)
    {
        var correlationId = GetOrCreateCorrelationId();
        Response.Headers["correlation-id"] = correlationId;
        var result = await _paymentService.UpdateAsync(id, request, correlationId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    private string GetOrCreateCorrelationId()
    {
        if (Request.Headers.TryGetValue("correlation-id", out var correlationId) && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        var generated = Guid.NewGuid().ToString("N");
        return generated;
    }
}