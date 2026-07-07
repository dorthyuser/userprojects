using currency_calculator.Models;

namespace currency_calculator.Repositories;

public interface ICurrencyTransferRepository
{
    List<SupportedCurrencyResponse> GetSupportedCurrencies();
    Dictionary<string, decimal> GetRates(string baseCurrency);
    decimal GetMarketRate(string fromCurrency, string toCurrency);
}