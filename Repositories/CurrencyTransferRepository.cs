using currency_calculator.Models;

namespace currency_calculator.Repositories;

public sealed class CurrencyTransferRepository : ICurrencyTransferRepository
{
    private readonly List<SupportedCurrencyResponse> _currencies = new()
    {
        new SupportedCurrencyResponse { Code = "USD", Symbol = "$", Name = "US Dollar" },
        new SupportedCurrencyResponse { Code = "INR", Symbol = "₹", Name = "Indian Rupee" },
        new SupportedCurrencyResponse { Code = "EUR", Symbol = "€", Name = "Euro" },
        new SupportedCurrencyResponse { Code = "GBP", Symbol = "£", Name = "British Pound" },
        new SupportedCurrencyResponse { Code = "AED", Symbol = "د.إ", Name = "UAE Dirham" },
        new SupportedCurrencyResponse { Code = "CAD", Symbol = "C$", Name = "Canadian Dollar" },
        new SupportedCurrencyResponse { Code = "AUD", Symbol = "A$", Name = "Australian Dollar" },
        new SupportedCurrencyResponse { Code = "SGD", Symbol = "S$", Name = "Singapore Dollar" },
        new SupportedCurrencyResponse { Code = "JPY", Symbol = "¥", Name = "Japanese Yen" }
    };

    private readonly Dictionary<string, decimal> _usdRates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = 1m,
        ["INR"] = 85m,
        ["EUR"] = 0.92m,
        ["GBP"] = 0.79m,
        ["AED"] = 3.67m,
        ["CAD"] = 1.36m,
        ["AUD"] = 1.52m,
        ["SGD"] = 1.34m,
        ["JPY"] = 157.2m
    };

    public List<SupportedCurrencyResponse> GetSupportedCurrencies() => _currencies;

    public Dictionary<string, decimal> GetRates(string baseCurrency)
    {
        var baseRate = _usdRates[baseCurrency.ToUpperInvariant()];
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in _usdRates)
        {
            result[kvp.Key] = Math.Round(kvp.Value / baseRate, 2);
        }
        return result;
    }

    public decimal GetMarketRate(string fromCurrency, string toCurrency)
    {
        var from = _usdRates[fromCurrency.ToUpperInvariant()];
        var to = _usdRates[toCurrency.ToUpperInvariant()];
        return Math.Round(to / from, 2);
    }
}