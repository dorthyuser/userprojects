using currency_calculator.Models;

namespace currency_calculator.Services;

public interface ICurrencyTransferService
{
    SupportedCurrenciesResponse GetSupportedCurrencies();
    HealthResponse GetHealth();
    ExchangeRateResponse GetRates(string? baseCurrency, string? fromCurrency, string? toCurrency);
    CurrencyTransferCalculationResponse Calculate(CurrencyTransferRequest request);
    CurrencyTransferValidationResponse Validate(CurrencyTransferRequest request);
}