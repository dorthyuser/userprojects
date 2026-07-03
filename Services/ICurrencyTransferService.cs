using currency_calculator.Models;

namespace currency_calculator.Services;

public interface ICurrencyTransferService
{
    Task<CurrenciesResponse> GetCurrenciesAsync(CancellationToken cancellationToken);
    Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken);
    Task<RatesResponse> GetRatesAsync(RatesQueryRequest request, CancellationToken cancellationToken);
    Task<CalculateResponse> CalculateAsync(CalculateRequest request, CancellationToken cancellationToken);
}