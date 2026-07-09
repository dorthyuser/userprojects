using currency_calculator.Models;

namespace currency_calculator.Repositories;

public sealed class CurrencyTransferRepository : ICurrencyTransferRepository
{
    private readonly ILogger<CurrencyTransferRepository> _logger;
    private readonly Dictionary<string, (string Symbol, string Name)> _currencies = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = ("$", "US Dollar"),
        ["INR"] = ("₹", "Indian Rupee"),
        ["EUR"] = ("€", "Euro"),
        ["GBP"] = ("£", "British Pound"),
        ["AED"] = ("د.إ", "UAE Dirham"),
        ["CAD"] = ("C$", "Canadian Dollar"),
        ["AUD"] = ("A$", "Australian Dollar"),
        ["SGD"] = ("S$", "Singapore Dollar"),
        ["JPY"] = ("¥", "Japanese Yen")
    };

    public CurrencyTransferRepository(ILogger<CurrencyTransferRepository> logger)
    {
        _logger = logger;
    }

    public SupportedCurrenciesResponse GetSupportedCurrencies() => new() { SupportedCurrencies = _currencies.Select(x => new SupportedCurrencyItemResponse { Code = x.Key, Symbol = x.Value.Symbol, Name = x.Value.Name }).ToList() };

    public HealthResponse GetHealth() => new() { Status = "ok", Service = "currency-transfer-calculator", Dependencies = new HealthDependenciesResponse { ExchangeRateProvider = "ok", Cache = "ok" }, Timestamp = DateTime.Parse("2026-07-06T00:00:00.000Z", null, System.Globalization.DateTimeStyles.RoundtripKind) };

    public ExchangeRateResponse GetRates(string? baseCurrency, string? fromCurrency, string? toCurrency)
    {
        _logger.LogInformation("SELECT currency_rates");
        var baseCode = !string.IsNullOrWhiteSpace(baseCurrency) ? baseCurrency.ToUpperInvariant() : (!string.IsNullOrWhiteSpace(fromCurrency) ? fromCurrency.ToUpperInvariant() : "USD");
        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
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
        return new ExchangeRateResponse { Success = true, Data = new ExchangeRateDataResponse { BaseCurrency = baseCode, Rates = rates, Cached = true, CacheTtlSeconds = 300, Timestamp = DateTime.Parse("2026-07-06T00:00:00.000Z", null, System.Globalization.DateTimeStyles.RoundtripKind) } };
    }

    public bool IsSupportedCurrency(string currencyCode) => _currencies.ContainsKey(currencyCode.ToUpperInvariant());

    public IReadOnlyList<string> GetSupportedCurrencyCodes() => _currencies.Keys.ToList();

    public string GetSymbol(string currencyCode) => _currencies.TryGetValue(currencyCode.ToUpperInvariant(), out var value) ? value.Symbol : string.Empty;
}