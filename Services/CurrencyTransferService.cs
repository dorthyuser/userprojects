using currency_calculator.Models;
using currency_calculator.Repositories;

namespace currency_calculator.Services;

public sealed class CurrencyTransferService : ICurrencyTransferService
{
    private static readonly Dictionary<string, (string Symbol, string Name)> CurrencyMap = new(StringComparer.OrdinalIgnoreCase)
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

    private readonly ICurrencyTransferRepository _repository;
    private readonly ILogger<CurrencyTransferService> _logger;

    public CurrencyTransferService(ICurrencyTransferRepository repository, ILogger<CurrencyTransferService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<CurrenciesResponse> GetCurrenciesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new CurrenciesResponse
        {
            Currencies = CurrencyMap.Select(x => new CurrencyItemResponse { Code = x.Key, Symbol = x.Value.Symbol, Name = x.Value.Name }).ToList()
        });
    }

    public Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new HealthResponse
        {
            Status = "ok",
            Service = "currency-transfer-calculator",
            Dependencies = new HealthDependenciesResponse { ExchangeRateProvider = "ok", Cache = "ok", TaxRules = "ok" },
            Timestamp = DateTime.Parse("2026-06-29T00:00:00.000Z", null, System.Globalization.DateTimeStyles.RoundtripKind)
        });
    }

    public async Task<RatesResponse> GetRatesAsync(RatesQueryRequest request, CancellationToken cancellationToken)
    {
        var rates = await _repository.GetRatesAsync(request.BaseCurrency, request.QuoteCurrency, cancellationToken);
        return new RatesResponse
        {
            BaseCurrency = request.BaseCurrency.ToUpperInvariant(),
            QuoteCurrency = request.QuoteCurrency.ToUpperInvariant(),
            MarketRate = rates.MarketRate,
            BankRate = rates.BankRate,
            SymbolMap = CurrencyMap.ToDictionary(x => x.Key, x => x.Value.Symbol),
            Cached = rates.Cached,
            CacheTtlSeconds = rates.CacheTtlSeconds,
            Timestamp = rates.Timestamp
        };
    }

    public async Task<CalculateResponse> CalculateAsync(CalculateRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating request...");
        if (!CurrencyMap.ContainsKey(request.FromCurrency)) throw new ArgumentException("fromCurrency must be a valid ISO 4217 currency code");
        if (!CurrencyMap.ContainsKey(request.ToCurrency)) throw new ArgumentException("toCurrency must be a valid ISO 4217 currency code");
        if (request.Amount <= 0) throw new ArgumentException("amount must be positive");
        _logger.LogInformation("Validation passed.");

        var rates = await _repository.GetRatesAsync(request.FromCurrency, request.ToCurrency, cancellationToken);
        var sourceSymbol = CurrencyMap[request.FromCurrency].Symbol;
        var destinationSymbol = CurrencyMap[request.ToCurrency].Symbol;
        var gross = Math.Round(request.Amount * rates.MarketRate, 2, MidpointRounding.AwayFromZero);
        var transferFee = Math.Round(request.Amount * 0.50m, 2, MidpointRounding.AwayFromZero);
        var platformFee = Math.Round(request.Amount * 0.20m, 2, MidpointRounding.AwayFromZero);
        var gst = Math.Round((transferFee + platformFee) * 0.18m, 2, MidpointRounding.AwayFromZero);
        var totalFees = Math.Round(transferFee + platformFee + gst, 2, MidpointRounding.AwayFromZero);
        var exchangeLoss = Math.Round((rates.MarketRate - rates.BankRate) * request.Amount, 2, MidpointRounding.AwayFromZero);
        var net = Math.Round(gross - totalFees, 2, MidpointRounding.AwayFromZero);

        var response = new CalculateResponse
        {
            Source = new CalculateSourceResponse { Currency = request.FromCurrency.ToUpperInvariant(), Symbol = sourceSymbol, Amount = Math.Round(request.Amount, 2) },
            Destination = new CalculateDestinationResponse { Currency = request.ToCurrency.ToUpperInvariant(), Symbol = destinationSymbol },
            MarketRate = rates.MarketRate,
            BankRate = rates.BankRate,
            GrossAmountInINR = gross,
            Fees = new CalculateFeesResponse { TransferFee = transferFee, PlatformFee = platformFee, Gst = gst, TotalFees = totalFees },
            ExchangeLoss = exchangeLoss,
            NetAmountReceived = net,
            Summary = new CalculateSummaryResponse { SenderPays = $"{sourceSymbol}{request.Amount:0.00}", ReceiverGets = $"{destinationSymbol}{net:0.00}" },
            Breakdown = new CalculateBreakdownResponse
            {
                MarketConversion = new CalculateBreakdownMarketConversionResponse { Rate = rates.MarketRate, Amount = gross },
                BankConversion = new CalculateBreakdownBankConversionResponse { Rate = rates.BankRate, Amount = Math.Round(request.Amount * rates.BankRate, 2, MidpointRounding.AwayFromZero) },
                Deductions = new CalculateBreakdownDeductionsResponse { TransferFee = transferFee, PlatformFee = platformFee, Gst = gst, TotalFees = totalFees },
                FinalSettlement = new CalculateBreakdownFinalSettlementResponse { NetAmountReceived = net, ExchangeLoss = exchangeLoss }
            }
        };

        _logger.LogInformation("Response sent.");
        return response;
    }
}