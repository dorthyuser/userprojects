using currency_calculator.Models;
using currency_calculator.Repositories;

namespace currency_calculator.Services;

public sealed class CurrencyTransferService : ICurrencyTransferService
{
    private readonly ICurrencyTransferRepository _repository;
    private readonly ILogger<CurrencyTransferService> _logger;

    public CurrencyTransferService(ICurrencyTransferRepository repository, ILogger<CurrencyTransferService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<SupportedCurrenciesResponse> GetCurrenciesAsync(CancellationToken cancellationToken) => Task.FromResult(new SupportedCurrenciesResponse { SupportedCurrencies = _repository.GetSupportedCurrencies() });
    public Task<SupportedCurrenciesListResponse> GetSupportedCurrenciesAsync(CancellationToken cancellationToken) => Task.FromResult(new SupportedCurrenciesListResponse { Currencies = _repository.GetSupportedCurrencies() });
    public Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken) => Task.FromResult(new HealthResponse { Status = "ok", Service = "currency-transfer-calculator", Dependencies = new HealthDependenciesResponse { ExchangeRateProvider = "ok", Cache = "ok" }, Timestamp = DateTimeOffset.Parse("2026-06-29T00:00:00.000Z") });
    public Task<RatesResponse> GetRatesAsync(string baseCurrency, CancellationToken cancellationToken) => Task.FromResult(_repository.GetRates(baseCurrency));

    public Task<ValidateResponse> ValidateAsync(CurrencyTransferRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating request...");
        var result = _repository.Validate(request);
        _logger.LogInformation("Validation passed.");
        return Task.FromResult(result);
    }

    public Task<CalculateResponse> CalculateAsync(CurrencyTransferRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating request...");
        var result = _repository.Calculate(request);
        _logger.LogInformation("Validation passed.");
        _logger.LogInformation("Response sent.");
        return Task.FromResult(result);
    }
}