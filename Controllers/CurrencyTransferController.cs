using currency_calculator.Models;
using currency_calculator.Services;
using Microsoft.AspNetCore.Mvc;

namespace currency_calculator.Controllers;

[ApiController]
[Route("api/v1/currency-transfer")]
public sealed class CurrencyTransferController : ControllerBase
{
    private readonly ICurrencyTransferService _service;
    private readonly ILogger<CurrencyTransferController> _logger;

    public CurrencyTransferController(ICurrencyTransferService service, ILogger<CurrencyTransferController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("currencies")]
    public async Task<ActionResult<SupportedCurrenciesResponse>> GetCurrencies([FromHeader(Name = "X-Request-Id")] string? requestId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP GET /api/v1/currency-transfer/currencies client={ClientId}", string.IsNullOrWhiteSpace(requestId) ? "missing" : "present");
        return Ok(await _service.GetCurrenciesAsync(cancellationToken));
    }

    [HttpGet("supported-currencies")]
    public async Task<ActionResult<SupportedCurrenciesListResponse>> GetSupportedCurrencies([FromHeader(Name = "X-Request-Id")] string? requestId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP GET /api/v1/currency-transfer/supported-currencies client={ClientId}", string.IsNullOrWhiteSpace(requestId) ? "missing" : "present");
        return Ok(await _service.GetSupportedCurrenciesAsync(cancellationToken));
    }

    [HttpGet("health")]
    public async Task<ActionResult<HealthResponse>> Health([FromHeader(Name = "X-Request-Id")] string? requestId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP GET /api/v1/currency-transfer/health client={ClientId}", string.IsNullOrWhiteSpace(requestId) ? "missing" : "present");
        return Ok(await _service.GetHealthAsync(cancellationToken));
    }

    [HttpGet("rates")]
    public async Task<ActionResult<RatesResponse>> GetRates([FromQuery] string baseCurrency = "USD", [FromHeader(Name = "X-Request-Id")] string? requestId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HTTP GET /api/v1/currency-transfer/rates client={ClientId}", string.IsNullOrWhiteSpace(requestId) ? "missing" : "present");
        return Ok(await _service.GetRatesAsync(baseCurrency, cancellationToken));
    }

    [HttpPost("validate")]
    public async Task<ActionResult<ValidateResponse>> Validate([FromBody] CurrencyTransferRequest request, [FromHeader(Name = "X-Request-Id")] string? requestId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP POST /api/v1/currency-transfer/validate client={ClientId}", string.IsNullOrWhiteSpace(requestId) ? "missing" : "present");
        return Ok(await _service.ValidateAsync(request, cancellationToken));
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<CalculateResponse>> Calculate([FromBody] CurrencyTransferRequest request, [FromHeader(Name = "X-Request-Id")] string? requestId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP POST /api/v1/currency-transfer/calculate client={ClientId}", string.IsNullOrWhiteSpace(requestId) ? "missing" : "present");
        return Ok(await _service.CalculateAsync(request, cancellationToken));
    }
}