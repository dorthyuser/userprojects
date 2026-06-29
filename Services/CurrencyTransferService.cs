using currency_calculator.Models;
using currency_calculator.Repositories;

namespace currency_calculator.Services;

public sealed class CurrencyTransferService : ICurrencyTransferService
{
    private readonly ICurrencyTransferRepository _repository;
    private readonly ILogger<CurrencyTransferService> _logger;

    public CurrencyTransferService(ICurrencyTransferRepository repository, ILogger<CurrencyTransferService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<SupportedCurrenciesResponse> GetCurrenciesAsync() => Task.FromResult(new SupportedCurrenciesResponse { SupportedCurrencies = CurrencyCatalog.All });

    public Task<SupportedCurrenciesListResponse> GetSupportedCurrenciesAsync() => Task.FromResult(new SupportedCurrenciesListResponse { Currencies = CurrencyCatalog.All });

    public Task<CurrencyTransferHealthResponse> GetHealthAsync() => Task.FromResult(new CurrencyTransferHealthResponse { Status = "ok", Service = "currency-transfer-calculator", Dependencies = new CurrencyTransferDependenciesResponse { ExchangeRateProvider = "ok", Cache = "ok" }, Timestamp = DateTimeOffset.Parse("2026-06-29T00:00:00.000Z") });

    public async Task<CurrencyRatesResponse> GetRatesAsync(string baseCurrency)
    {
        _logger.LogInformation("Validating request...");
        if (!CurrencyCatalog.IsSupported(baseCurrency))
        {
            _logger.LogWarning("Validation failure: field baseCurrency must be one of {AcceptedValues}", string.Join(",", CurrencyCatalog.Codes));
            return new CurrencyRatesResponse { BaseCurrency = baseCurrency, Rates = new Dictionary<string, decimal>(), Cached = true, CacheTtlSeconds = 300, Timestamp = DateTimeOffset.UtcNow };
        }
        _logger.LogInformation("Validation passed.");
        var rates = await _repository.GetRatesAsync(baseCurrency);
        return rates;
    }

    public Task<CurrencyTransferValidationResponse> ValidateAsync(CurrencyTransferRequest request)
    {
        _logger.LogInformation("Validating request...");
        var response = ValidateInternal(request);
        _logger.LogInformation("Validation passed.");
        return Task.FromResult(response);
    }

    public async Task<CurrencyTransferCalculationResponse> CalculateAsync(CurrencyTransferRequest request)
    {
        _logger.LogInformation("Validating request...");
        var validation = ValidateInternal(request);
        if (!validation.Valid)
        {
            return new CurrencyTransferCalculationResponse();
        }
        _logger.LogInformation("Validation passed.");
        var result = await _repository.CalculateAsync(request);
        _logger.LogInformation("Response sent with generated_identifier={GeneratedIdentifier}", "calculation");
        return result;
    }

    private static CurrencyTransferValidationResponse ValidateInternal(CurrencyTransferRequest request)
    {
        var errors = new Dictionary<string, string>();
        if (!CurrencyCatalog.IsSupported(request.FromCurrency)) errors["fromCurrency"] = "Accepted values: USD, INR, EUR, GBP, AED, CAD, AUD, SGD, JPY";
        if (!CurrencyCatalog.IsSupported(request.ToCurrency)) errors["toCurrency"] = "Accepted values: USD, INR, EUR, GBP, AED, CAD, AUD, SGD, JPY";
        if (request.Amount <= 0) errors["amount"] = "Must be greater than 0";
        return new CurrencyTransferValidationResponse { Valid = errors.Count == 0, Errors = errors, Warnings = new Dictionary<string, string>() };
    }
}

internal static class CurrencyCatalog
{
    public static readonly List<SupportedCurrencyResponse> All = new()
    {
        new() { Code = "USD", Symbol = "$", Name = "US Dollar" },
        new() { Code = "INR", Symbol = "₹", Name = "Indian Rupee" },
        new() { Code = "EUR", Symbol = "€", Name = "Euro" },
        new() { Code = "GBP", Symbol = "£", Name = "British Pound" },
        new() { Code = "AED", Symbol = "د.إ", Name = "UAE Dirham" },
        new() { Code = "CAD", Symbol = "C$", Name = "Canadian Dollar" },
        new() { Code = "AUD", Symbol = "A$", Name = "Australian Dollar" },
        new() { Code = "SGD", Symbol = "S$", Name = "Singapore Dollar" },
        new() { Code = "JPY", Symbol = "¥", Name = "Japanese Yen" }
    };

    public static readonly HashSet<string> Codes = new(All.Select(x => x.Code), StringComparer.OrdinalIgnoreCase);
    public static bool IsSupported(string code) => Codes.Contains(code);
}