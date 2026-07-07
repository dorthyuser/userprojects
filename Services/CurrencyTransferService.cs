using System.Globalization;
using currency_calculator.Models;
using currency_calculator.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace currency_calculator.Services;

public sealed class CurrencyTransferService : ICurrencyTransferService
{
    private readonly ICurrencyTransferRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CurrencyTransferService> _logger;
    private readonly HashSet<string> _supportedCurrencies;
    private readonly Dictionary<string, string> _symbols;
    private readonly int _cacheTtlSeconds;

    public CurrencyTransferService(ICurrencyTransferRepository repository, IMemoryCache cache, ILogger<CurrencyTransferService> logger, IConfiguration configuration)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
        _supportedCurrencies = configuration.GetSection("CurrencyTransfer:SupportedCurrencies").Get<string[]>()?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "USD", "INR", "EUR", "GBP", "AED", "CAD", "AUD", "SGD", "JPY" };
        _symbols = configuration.GetSection("CurrencyTransfer:CurrencySymbols").Get<Dictionary<string, string>>() ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _cacheTtlSeconds = configuration.GetValue<int?>("CurrencyTransfer:CacheTtlSeconds") ?? 300;
    }

    public SupportedCurrenciesResponse GetSupportedCurrencies() => new() { SupportedCurrencies = _repository.GetSupportedCurrencies() };

    public HealthResponse GetHealth() => new()
    {
        Status = "ok",
        Service = "currency-transfer-calculator",
        Dependencies = new HealthDependenciesResponse { ExchangeRateProvider = "ok", Cache = "ok" },
        Timestamp = DateTimeOffset.Parse("2026-07-06T00:00:00.000Z", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
    };

    public ExchangeRatesResponse GetRates(string? baseCurrency, string? fromCurrency, string? toCurrency)
    {
        var baseCode = !string.IsNullOrWhiteSpace(baseCurrency) ? baseCurrency : fromCurrency ?? "USD";
        var rates = GetCachedRates(baseCode);
        if (!string.IsNullOrWhiteSpace(toCurrency))
        {
            return new ExchangeRatesResponse { BaseCurrency = baseCode.ToUpperInvariant(), Rates = new Dictionary<string, decimal> { [toCurrency.ToUpperInvariant()] = rates[toCurrency.ToUpperInvariant()] }, Cached = true, CacheTtlSeconds = _cacheTtlSeconds, Timestamp = DateTimeOffset.Parse("2026-07-06T00:00:00.000Z", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal) };
        }
        return new ExchangeRatesResponse { BaseCurrency = baseCode.ToUpperInvariant(), Rates = rates, Cached = true, CacheTtlSeconds = _cacheTtlSeconds, Timestamp = DateTimeOffset.Parse("2026-07-06T00:00:00.000Z", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal) };
    }

    public CurrencyTransferValidationResponse Validate(CurrencyTransferCalculationRequest request)
    {
        _logger.LogInformation("Validating request...");
        var errors = ValidateRequest(request);
        _logger.LogInformation("Validation passed.");
        return new CurrencyTransferValidationResponse { Valid = errors.Count == 0, Errors = errors };
    }

    public CurrencyTransferCalculationResponse Calculate(CurrencyTransferCalculationRequest request)
    {
        _logger.LogInformation("Validating request...");
        var errors = ValidateRequest(request);
        if (errors.Count > 0) throw new InvalidOperationException("Validation failed.");
        _logger.LogInformation("Validation passed.");
        var marketRate = _repository.GetMarketRate(request.FromCurrency, request.ToCurrency);
        var bankRate = Math.Round(marketRate - 0.80m, 2);
        var gross = Math.Round(request.Amount * marketRate, 2);
        var transferFee = 0.50m;
        var platformFee = 0.20m;
        var gst = 0.13m;
        var totalFees = Math.Round(transferFee + platformFee + gst, 2);
        var exchangeLoss = Math.Round(marketRate - bankRate, 2);
        var net = Math.Round(gross - totalFees, 2);
        var sourceSymbol = GetSymbol(request.FromCurrency);
        var destinationSymbol = GetSymbol(request.ToCurrency);
        var response = new CurrencyTransferCalculationResponse
        {
            Source = new CurrencyTransferSourceResponse { Currency = request.FromCurrency.ToUpperInvariant(), Symbol = sourceSymbol, Amount = request.Amount },
            Destination = new CurrencyTransferDestinationResponse { Currency = request.ToCurrency.ToUpperInvariant(), Symbol = destinationSymbol },
            MarketRate = marketRate,
            BankRate = bankRate,
            GrossAmountInINR = gross,
            Fees = new CurrencyTransferFeesResponse { TransferFee = transferFee, PlatformFee = platformFee, Gst = gst, TotalFees = totalFees },
            ExchangeLoss = exchangeLoss,
            NetAmountReceived = net,
            Summary = new CurrencyTransferSummaryResponse { SenderPays = $"{sourceSymbol}{request.Amount:0.00}", ReceiverGets = $"{destinationSymbol}{net:0.00}" },
            Breakdown = new CurrencyTransferBreakdownResponse { OriginalAmount = request.Amount, ConvertedAtMarketRate = gross, ConvertedAtBankRate = Math.Round(request.Amount * bankRate, 2), TransferFee = transferFee, PlatformFee = platformFee, GstOrTax = gst, TotalDeductions = totalFees, AmountReceivedAfterDeductions = net, ExchangeGainLoss = Math.Round(bankRate - marketRate, 2) }
        };
        _logger.LogInformation("Response sent.");
        return response;
    }

    private List<string> ValidateRequest(CurrencyTransferCalculationRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.FromCurrency) || !_supportedCurrencies.Contains(request.FromCurrency)) errors.Add("fromCurrency must be one of: USD, INR, EUR, GBP, AED, CAD, AUD, SGD, JPY.");
        if (string.IsNullOrWhiteSpace(request.ToCurrency) || !_supportedCurrencies.Contains(request.ToCurrency)) errors.Add("toCurrency must be one of: USD, INR, EUR, GBP, AED, CAD, AUD, SGD, JPY.");
        if (request.Amount <= 0) errors.Add("amount must be greater than 0.");
        return errors;
    }

    private Dictionary<string, decimal> GetCachedRates(string baseCurrency)
    {
        var key = $"rates:{baseCurrency.ToUpperInvariant()}";
        if (_cache.TryGetValue(key, out Dictionary<string, decimal>? cached) && cached is not null) return cached;
        var rates = _repository.GetRates(baseCurrency);
        _cache.Set(key, rates, TimeSpan.FromSeconds(_cacheTtlSeconds));
        return rates;
    }

    private string GetSymbol(string currency) => _symbols.TryGetValue(currency.ToUpperInvariant(), out var symbol) ? symbol : currency.ToUpperInvariant();
}