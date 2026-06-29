using currency_calculator.Models;

namespace currency_calculator.Services;

public interface ICurrencyTransferService
{
    Task<SupportedCurrenciesResponse> GetCurrenciesAsync();
    Task<SupportedCurrenciesListResponse> GetSupportedCurrenciesAsync();
    Task<CurrencyTransferHealthResponse> GetHealthAsync();
    Task<CurrencyRatesResponse> GetRatesAsync(string baseCurrency);
    Task<CurrencyTransferValidationResponse> ValidateAsync(CurrencyTransferRequest request);
    Task<CurrencyTransferCalculationResponse> CalculateAsync(CurrencyTransferRequest request);
}