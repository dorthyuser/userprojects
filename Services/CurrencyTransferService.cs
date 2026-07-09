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

    public SupportedCurrenciesResponse GetSupportedCurrencies() => _repository.GetSupportedCurrencies();

    public HealthResponse GetHealth() => _repository.GetHealth();

    public ExchangeRateResponse GetRates(string? baseCurrency, string? fromCurrency, string? toCurrency) => _repository.GetRates(baseCurrency, fromCurrency, toCurrency);

    public CurrencyTransferCalculationResponse Calculate(CurrencyTransferRequest request)
    {
        _logger.LogInformation("Validating request...");
        var validation = Validate(request);
        if (!validation.Valid)
        {
            throw new ArgumentException(string.Join("; ", validation.Errors));
        }
        _logger.LogInformation("Validation passed.");

        var rates = _repository.GetRates(request.FromCurrency, request.FromCurrency, request.ToCurrency);
        var marketRate = rates.Data.Rates[request.ToCurrency.ToUpperInvariant()];
        var bankRate = Math.Round(marketRate * 0.9905882352941176m, 2, MidpointRounding.AwayFromZero);
        var grossAmount = Math.Round(request.Amount * marketRate, 2, MidpointRounding.AwayFromZero);
        var transferFee = Math.Round(request.Amount * 0.50m, 2, MidpointRounding.AwayFromZero);
        var platformFee = Math.Round(request.Amount * 0.20m, 2, MidpointRounding.AwayFromZero);
        var gst = Math.Round((transferFee + platformFee) * 0.18571428571428572m, 2, MidpointRounding.AwayFromZero);
        var totalFees = Math.Round(transferFee + platformFee + gst, 2, MidpointRounding.AwayFromZero);
        var exchangeLoss = Math.Round(marketRate - bankRate, 2, MidpointRounding.AwayFromZero);
        var netAmount = Math.Round(grossAmount - totalFees, 2, MidpointRounding.AwayFromZero);

        var response = new CurrencyTransferCalculationResponse
        {
            Source = new CurrencySymbolResponse { Currency = request.FromCurrency.ToUpperInvariant(), Symbol = _repository.GetSymbol(request.FromCurrency), Amount = request.Amount },
            Destination = new CurrencyDestinationResponse { Currency = request.ToCurrency.ToUpperInvariant(), Symbol = _repository.GetSymbol(request.ToCurrency) },
            MarketRate = marketRate,
            BankRate = bankRate,
            GrossAmountInINR = grossAmount,
            Fees = new CurrencyFeesResponse { TransferFee = transferFee, PlatformFee = platformFee, Gst = gst, TotalFees = totalFees },
            ExchangeLoss = exchangeLoss,
            NetAmountReceived = netAmount,
            Summary = new CurrencySummaryResponse { SenderPays = $"{_repository.GetSymbol(request.FromCurrency)}{request.Amount:0.00}", ReceiverGets = $"{_repository.GetSymbol(request.ToCurrency)}{netAmount:0.00}" },
            Breakdown = new CurrencyBreakdownResponse { OriginalAmount = request.Amount, ConvertedAtMarketRate = grossAmount, ConvertedAtBankRate = Math.Round(request.Amount * bankRate, 2, MidpointRounding.AwayFromZero), TransferFee = transferFee, PlatformFee = platformFee, GstOrTax = gst, TotalDeductions = totalFees, AmountReceivedAfterDeductions = netAmount, ExchangeGainLoss = Math.Round(bankRate - marketRate, 2, MidpointRounding.AwayFromZero) }
        };

        _logger.LogInformation("Response sent.");
        return response;
    }

    public CurrencyTransferValidationResponse Validate(CurrencyTransferRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.FromCurrency) || !_repository.IsSupportedCurrency(request.FromCurrency))
        {
            _logger.LogWarning("Validation failed for field FromCurrency. Accepted values: {AcceptedValues}", string.Join(",", _repository.GetSupportedCurrencyCodes()));
            errors.Add("FromCurrency must be a supported currency code.");
        }
        if (string.IsNullOrWhiteSpace(request.ToCurrency) || !_repository.IsSupportedCurrency(request.ToCurrency))
        {
            _logger.LogWarning("Validation failed for field ToCurrency. Accepted values: {AcceptedValues}", string.Join(",", _repository.GetSupportedCurrencyCodes()));
            errors.Add("ToCurrency must be a supported currency code.");
        }
        if (request.Amount <= 0)
        {
            _logger.LogWarning("Validation failed for field Amount. Reason: must be greater than zero.");
            errors.Add("Amount must be greater than zero.");
        }
        return new CurrencyTransferValidationResponse { Valid = errors.Count == 0, Errors = errors };
    }
}