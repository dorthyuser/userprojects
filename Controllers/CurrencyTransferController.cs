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
    public async Task<ActionResult<SupportedCurrenciesResponse>> GetCurrencies([FromHeader(Name = "X-Request-Id")] string? requestId)
    {
        _logger.LogInformation("HTTP GET /api/v1/currency-transfer/currencies client_identifier={ClientIdentifier}", string.IsNullOrWhiteSpace(requestId) ? "missing" : "present");
        var result = await _service.GetCurrenciesAsync();
        return Ok(result);
    }

    [HttpGet("supported-currencies")]
    public async Task<ActionResult<SupportedCurrenciesListResponse>> GetSupportedCurrencies()
    {
        _logger.LogInformation("HTTP GET /api/v1/currency-transfer/supported-currencies client_identifier={ClientIdentifier}", "present");
        var result = await _service.GetSupportedCurrenciesAsync();
        return Ok(result);
    }

    [HttpGet("health")]
    public async Task<ActionResult<CurrencyTransferHealthResponse>> GetHealth([FromHeader(Name = "X-Request-Id")] string? requestId)
    {
        _logger.LogInformation("HTTP GET /api/v1/currency-transfer/health client_identifier={ClientIdentifier}", string.IsNullOrWhiteSpace(requestId) ? "missing" : "present");
        var result = await _service.GetHealthAsync();
        return Ok(result);
    }

    [HttpGet("rates")]
    public async Task<ActionResult<CurrencyRatesResponse>> GetRates([FromQuery] string baseCurrency, [FromHeader(Name = "X-Request-Id")] string? requestId)
    {
        _logger.LogInformation("HTTP GET /api/v1/currency-transfer/rates client_identifier={ClientIdentifier}", string.IsNullOrWhiteSpace(requestId) ? "missing" : "present");
        var result = await _service.GetRatesAsync(baseCurrency);
        return Ok(result);
    }

    [HttpPost("validate")]
    public async Task<ActionResult<CurrencyTransferValidationResponse>> Validate([FromBody] CurrencyTransferRequest request, [FromHeader(Name = "X-Request-Id")] string? requestId)
    {
        _logger.LogInformation("HTTP POST /api/v1/currency-transfer/validate client_identifier={ClientIdentifier}", string.IsNullOrWhiteSpace(requestId) ? "missing" : "present");
        var result = await _service.ValidateAsync(request);
        return Ok(result);
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<CurrencyTransferCalculationResponse>> Calculate([FromBody] CurrencyTransferRequest request, [FromHeader(Name = "X-Request-Id")] string? requestId)
    {
        _logger.LogInformation("HTTP POST /api/v1/currency-transfer/calculate client_identifier={ClientIdentifier}", string.IsNullOrWhiteSpace(requestId) ? "missing" : "present");
        var result = await _service.CalculateAsync(request);
        return Ok(result);
    }
}