using currency_calculator.Models;

namespace currency_calculator.Services;

public interface ICurrencyTransferService
{
    SupportedCurrenciesResponse GetSupportedCurrencies();
    HealthResponse GetHealth();
    ExchangeRatesResponse GetRates(string? baseCurrency, string? fromCurrency, string? toCurrency);
    CurrencyTransferCalculationResponse Calculate(CurrencyTransferCalculationRequest request);
    CurrencyTransferValidationResponse Validate(CurrencyTransferCalculationRequest request);
}