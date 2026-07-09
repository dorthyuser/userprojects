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
    public ActionResult<SupportedCurrenciesResponse> GetCurrencies()
    {
        _logger.LogInformation("HTTP GET /api/v1/currency-transfer/currencies client={ClientId}", Request.Headers["client_id"].ToString());
        return Ok(_service.GetSupportedCurrencies());
    }

    [HttpGet("health")]
    public ActionResult<HealthResponse> GetHealth()
    {
        _logger.LogInformation("HTTP GET /api/v1/currency-transfer/health client={ClientId}", Request.Headers["client_id"].ToString());
        return Ok(_service.GetHealth());
    }

    [HttpGet("rates")]
    public ActionResult<ExchangeRateResponse> GetRates([FromQuery] string? baseCurrency, [FromQuery] string? fromCurrency, [FromQuery] string? toCurrency)
    {
        _logger.LogInformation("HTTP GET /api/v1/currency-transfer/rates client={ClientId}", Request.Headers["client_id"].ToString());
        return Ok(_service.GetRates(baseCurrency, fromCurrency, toCurrency));
    }

    [HttpPost("calculate")]
    public ActionResult<CurrencyTransferCalculationResponse> Calculate([FromBody] CurrencyTransferRequest request)
    {
        _logger.LogInformation("HTTP POST /api/v1/currency-transfer/calculate client={ClientId}", Request.Headers["client_id"].ToString());
        return Ok(_service.Calculate(request));
    }

    [HttpPost("validate")]
    public ActionResult<CurrencyTransferValidationResponse> Validate([FromBody] CurrencyTransferRequest request)
    {
        _logger.LogInformation("HTTP POST /api/v1/currency-transfer/validate client={ClientId}", Request.Headers["client_id"].ToString());
        return Ok(_service.Validate(request));
    }
}