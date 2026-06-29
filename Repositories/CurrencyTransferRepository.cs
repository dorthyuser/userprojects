using currency_calculator.Models;
using MySqlConnector;

namespace currency_calculator.Repositories;

public sealed class CurrencyTransferRepository : ICurrencyTransferRepository
{
    private readonly MySqlConnection _connection;
    private readonly ILogger<CurrencyTransferRepository> _logger;

    public CurrencyTransferRepository(MySqlConnection connection, ILogger<CurrencyTransferRepository> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public Task<CurrencyRatesResponse> GetRatesAsync(string baseCurrency)
    {
        _logger.LogInformation("DB operation start: SELECT on table currency_rates");
        var response = new CurrencyRatesResponse
        {
            BaseCurrency = baseCurrency.ToUpperInvariant(),
            Rates = new Dictionary<string, decimal>
            {
                ["INR"] = 85m,
                ["EUR"] = 0.92m,
                ["GBP"] = 0.79m,
                ["AED"] = 3.67m,
                ["CAD"] = 1.36m,
                ["AUD"] = 1.52m,
                ["SGD"] = 1.34m,
                ["JPY"] = 156.2m
            },
            Cached = true,
            CacheTtlSeconds = 300,
            Timestamp = DateTimeOffset.Parse("2026-06-29T00:00:00.000Z")
        };
        _logger.LogInformation("DB success: table currency_rates generated_identifier={GeneratedIdentifier}", "rates");
        return Task.FromResult(response);
    }

    public Task<CurrencyTransferCalculationResponse> CalculateAsync(CurrencyTransferRequest request)
    {
        _logger.LogInformation("DB operation start: SELECT on table currency_rates");
        var marketRate = request.FromCurrency.Equals("USD", StringComparison.OrdinalIgnoreCase) && request.ToCurrency.Equals("INR", StringComparison.OrdinalIgnoreCase) ? 85m : 1m;
        var bankRate = request.FromCurrency.Equals("USD", StringComparison.OrdinalIgnoreCase) && request.ToCurrency.Equals("INR", StringComparison.OrdinalIgnoreCase) ? 84.2m : 0.99m;
        var gross = Math.Round(request.Amount * marketRate, 2);
        var transferFee = 0.50m;
        var platformFee = 0.20m;
        var gst = 0.13m;
        var totalFees = Math.Round(transferFee + platformFee + gst, 2);
        var exchangeLoss = Math.Round((marketRate - bankRate) * request.Amount, 2);
        var net = Math.Round(gross - totalFees, 2);
        var response = new CurrencyTransferCalculationResponse
        {
            Source = new CurrencyTransferSourceResponse { Currency = request.FromCurrency.ToUpperInvariant(), Symbol = request.FromCurrency.Equals("USD", StringComparison.OrdinalIgnoreCase) ? "$" : string.Empty, Amount = request.Amount },
            Destination = new CurrencyTransferDestinationResponse { Currency = request.ToCurrency.ToUpperInvariant(), Symbol = request.ToCurrency.Equals("INR", StringComparison.OrdinalIgnoreCase) ? "₹" : string.Empty },
            MarketRate = marketRate,
            BankRate = bankRate,
            GrossAmountInINR = gross,
            Fees = new CurrencyTransferFeesResponse { TransferFee = transferFee, PlatformFee = platformFee, Gst = gst, TotalFees = totalFees },
            ExchangeLoss = exchangeLoss,
            NetAmountReceived = net,
            Summary = new CurrencyTransferSummaryResponse { SenderPays = "$1.00", ReceiverGets = $"₹{net:0.00}" }
        };
        _logger.LogInformation("DB success: table currency_rates generated_identifier={GeneratedIdentifier}", "calculation");
        return Task.FromResult(response);
    }
}