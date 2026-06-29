using currency_calculator.Models;
using MySqlConnector;

namespace currency_calculator.Repositories;

public sealed class CurrencyTransferRepository : ICurrencyTransferRepository
{
    private static readonly List<SupportedCurrencyItemResponse> Supported =
    [
        new() { Code = "USD", Symbol = "$", Name = "US Dollar" },
        new() { Code = "INR", Symbol = "₹", Name = "Indian Rupee" },
        new() { Code = "EUR", Symbol = "€", Name = "Euro" },
        new() { Code = "GBP", Symbol = "£", Name = "British Pound" },
        new() { Code = "AED", Symbol = "د.إ", Name = "UAE Dirham" },
        new() { Code = "CAD", Symbol = "C$", Name = "Canadian Dollar" },
        new() { Code = "AUD", Symbol = "A$", Name = "Australian Dollar" },
        new() { Code = "SGD", Symbol = "S$", Name = "Singapore Dollar" },
        new() { Code = "JPY", Symbol = "¥", Name = "Japanese Yen" }
    ];

    private readonly MySqlDataSource _dataSource;
    private readonly ILogger<CurrencyTransferRepository> _logger;

    public CurrencyTransferRepository(MySqlDataSource dataSource, ILogger<CurrencyTransferRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public List<SupportedCurrencyItemResponse> GetSupportedCurrencies() => Supported;

    public RatesResponse GetRates(string baseCurrency) => new() { BaseCurrency = baseCurrency.ToUpperInvariant(), Rates = new Dictionary<string, decimal> { ["INR"] = 85m, ["EUR"] = 0.92m, ["GBP"] = 0.79m, ["AED"] = 3.67m, ["CAD"] = 1.36m, ["AUD"] = 1.52m, ["SGD"] = 1.34m, ["JPY"] = 156.2m }, Cached = true, CacheTtlSeconds = 300, Timestamp = DateTimeOffset.Parse("2026-06-29T00:00:00.000Z") };

    public ValidateResponse Validate(CurrencyTransferRequest request)
    {
        var errors = new Dictionary<string, string>();
        var warnings = new Dictionary<string, string>();
        if (!Supported.Any(x => x.Code == request.FromCurrency.ToUpperInvariant())) errors["fromCurrency"] = "Accepted values: USD, INR, EUR, GBP, AED, CAD, AUD, SGD, JPY";
        if (!Supported.Any(x => x.Code == request.ToCurrency.ToUpperInvariant())) errors["toCurrency"] = "Accepted values: USD, INR, EUR, GBP, AED, CAD, AUD, SGD, JPY";
        if (request.Amount <= 0) errors["amount"] = "Must be greater than 0";
        return new ValidateResponse { Valid = errors.Count == 0, Errors = errors, Warnings = warnings };
    }

    public CalculateResponse Calculate(CurrencyTransferRequest request)
    {
        var marketRate = 85m;
        var bankRate = 84.2m;
        var gross = Math.Round(request.Amount * marketRate, 2);
        var transferFee = 0.50m;
        var platformFee = 0.20m;
        var gst = 0.13m;
        var totalFees = Math.Round(transferFee + platformFee + gst, 2);
        var exchangeLoss = Math.Round((marketRate - bankRate) * request.Amount, 2);
        var net = Math.Round(gross - totalFees, 2);
        return new CalculateResponse { Source = new CurrencyAmountResponse { Currency = request.FromCurrency.ToUpperInvariant(), Symbol = "$", Amount = request.Amount }, Destination = new CurrencyOnlyResponse { Currency = request.ToCurrency.ToUpperInvariant(), Symbol = "₹" }, MarketRate = marketRate, BankRate = bankRate, GrossAmountInINR = gross, Fees = new FeesResponse { TransferFee = transferFee, PlatformFee = platformFee, Gst = gst, TotalFees = totalFees }, ExchangeLoss = exchangeLoss, NetAmountReceived = net, Summary = new SummaryResponse { SenderPays = "$1.00", ReceiverGets = $"₹{net:0.00}" } };
    }
}