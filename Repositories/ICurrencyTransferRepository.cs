using currency_calculator.Models;

namespace currency_calculator.Repositories;

public interface ICurrencyTransferRepository
{
    Task<CurrencyRatesResponse> GetRatesAsync(string baseCurrency);
    Task<CurrencyTransferCalculationResponse> CalculateAsync(CurrencyTransferRequest request);
}