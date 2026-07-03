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
    public async Task<ActionResult<CurrenciesResponse>> GetCurrencies(CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP GET /api/v1/currency-transfer/currencies client={ClientId}", Request.Headers["X-Request-Id"].ToString());
        var result = await _service.GetCurrenciesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("health")]
    public async Task<ActionResult<HealthResponse>> GetHealth(CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP GET /api/v1/currency-transfer/health client={ClientId}", Request.Headers["X-Request-Id"].ToString());
        var result = await _service.GetHealthAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("rates")]
    public async Task<ActionResult<RatesResponse>> GetRates([FromQuery] RatesQueryRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP GET /api/v1/currency-transfer/rates client={ClientId}", Request.Headers["X-Request-Id"].ToString());
        var result = await _service.GetRatesAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("swagger")]
    public ActionResult<SwaggerResponse> GetSwagger()
    {
        _logger.LogInformation("HTTP GET /api/v1/currency-transfer/swagger client={ClientId}", Request.Headers["X-Request-Id"].ToString());
        return Ok(new SwaggerResponse { OpenApiVersion = "3.0.3", Title = "Currency Transfer Calculator API", Version = "1.0.0", DocumentationUrl = "https://ai2dev.com/api-docs" });
    }

    [HttpGet("tests")]
    public ActionResult<TestsResponse> GetTests()
    {
        _logger.LogInformation("HTTP GET /api/v1/currency-transfer/tests client={ClientId}", Request.Headers["X-Request-Id"].ToString());
        return Ok(new TestsResponse());
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<CalculateResponse>> Calculate([FromBody] CalculateRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP POST /api/v1/currency-transfer/calculate client={ClientId}", Request.Headers["X-Request-Id"].ToString());
        var result = await _service.CalculateAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("validate")]
    public ActionResult<ValidateResponse> Validate([FromBody] ValidateRequest request)
    {
        _logger.LogInformation("HTTP POST /api/v1/currency-transfer/validate client={ClientId}", Request.Headers["X-Request-Id"].ToString());
        return Ok(new ValidateResponse { Valid = true, Errors = new List<string>() });
    }
}