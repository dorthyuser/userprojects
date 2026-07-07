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
        _logger.LogInformation("HTTP {Method} {Route} client_identifier={ClientIdentifier}", "GET", "/api/v1/currency-transfer/currencies", "");
        return Ok(_service.GetSupportedCurrencies());
    }

    [HttpGet("health")]
    public ActionResult<HealthResponse> GetHealth()
    {
        _logger.LogInformation("HTTP {Method} {Route} client_identifier={ClientIdentifier}", "GET", "/api/v1/currency-transfer/health", "");
        return Ok(_service.GetHealth());
    }

    [HttpGet("rates")]
    public ActionResult<ExchangeRatesResponse> GetRates([FromQuery] string? baseCurrency, [FromQuery] string? fromCurrency, [FromQuery] string? toCurrency)
    {
        _logger.LogInformation("HTTP {Method} {Route} client_identifier={ClientIdentifier}", "GET", "/api/v1/currency-transfer/rates", "");
        return Ok(_service.GetRates(baseCurrency, fromCurrency, toCurrency));
    }

    [HttpPost("calculate")]
    public ActionResult<CurrencyTransferCalculationResponse> Calculate([FromBody] CurrencyTransferCalculationRequest request)
    {
        _logger.LogInformation("HTTP {Method} {Route} client_identifier={ClientIdentifier}", "POST", "/api/v1/currency-transfer/calculate", "");
        return Ok(_service.Calculate(request));
    }

    [HttpPost("validate")]
    public ActionResult<CurrencyTransferValidationResponse> Validate([FromBody] CurrencyTransferCalculationRequest request)
    {
        _logger.LogInformation("HTTP {Method} {Route} client_identifier={ClientIdentifier}", "POST", "/api/v1/currency-transfer/validate", "");
        return Ok(_service.Validate(request));
    }
}