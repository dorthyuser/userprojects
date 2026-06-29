using currency_calculator.Models;

namespace currency_calculator.Services;

public interface ICurrencyTransferService
{
    Task<SupportedCurrenciesResponse> GetCurrenciesAsync(CancellationToken cancellationToken);
    Task<SupportedCurrenciesListResponse> GetSupportedCurrenciesAsync(CancellationToken cancellationToken);
    Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken);
    Task<RatesResponse> GetRatesAsync(string baseCurrency, CancellationToken cancellationToken);
    Task<ValidateResponse> ValidateAsync(CurrencyTransferRequest request, CancellationToken cancellationToken);
    Task<CalculateResponse> CalculateAsync(CurrencyTransferRequest request, CancellationToken cancellationToken);
}