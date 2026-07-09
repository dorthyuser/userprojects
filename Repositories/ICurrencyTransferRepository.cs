using currency_calculator.Models;

namespace currency_calculator.Repositories;

public interface ICurrencyTransferRepository
{
    SupportedCurrenciesResponse GetSupportedCurrencies();
    HealthResponse GetHealth();
    ExchangeRateResponse GetRates(string? baseCurrency, string? fromCurrency, string? toCurrency);
    bool IsSupportedCurrency(string currencyCode);
    IReadOnlyList<string> GetSupportedCurrencyCodes();
    string GetSymbol(string currencyCode);
}