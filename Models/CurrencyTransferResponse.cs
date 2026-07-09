using System.Text.Json.Serialization;

namespace currency_calculator.Models;

public sealed class SupportedCurrencyItemResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public sealed class SupportedCurrenciesResponse
{
    [JsonPropertyName("supportedCurrencies")]
    public List<SupportedCurrencyItemResponse> SupportedCurrencies { get; set; } = new();
}

public sealed class HealthDependenciesResponse
{
    [JsonPropertyName("exchangeRateProvider")]
    public string ExchangeRateProvider { get; set; } = string.Empty;

    [JsonPropertyName("cache")]
    public string Cache { get; set; } = string.Empty;
}

public sealed class HealthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("service")]
    public string Service { get; set; } = string.Empty;

    [JsonPropertyName("dependencies")]
    public HealthDependenciesResponse Dependencies { get; set; } = new();

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

public sealed class ExchangeRateDataResponse
{
    [JsonPropertyName("baseCurrency")]
    public string BaseCurrency { get; set; } = string.Empty;

    [JsonPropertyName("rates")]
    public Dictionary<string, decimal> Rates { get; set; } = new();

    [JsonPropertyName("cached")]
    public bool Cached { get; set; }

    [JsonPropertyName("cacheTtlSeconds")]
    public int CacheTtlSeconds { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

public sealed class ExchangeRateResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public ExchangeRateDataResponse Data { get; set; } = new();
}

public sealed class CurrencySymbolResponse
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
}

public sealed class CurrencyDestinationResponse
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;
}

public sealed class CurrencyFeesResponse
{
    [JsonPropertyName("transferFee")]
    public decimal TransferFee { get; set; }

    [JsonPropertyName("platformFee")]
    public decimal PlatformFee { get; set; }

    [JsonPropertyName("gst")]
    public decimal Gst { get; set; }

    [JsonPropertyName("totalFees")]
    public decimal TotalFees { get; set; }
}

public sealed class CurrencySummaryResponse
{
    [JsonPropertyName("senderPays")]
    public string SenderPays { get; set; } = string.Empty;

    [JsonPropertyName("receiverGets")]
    public string ReceiverGets { get; set; } = string.Empty;
}

public sealed class CurrencyBreakdownResponse
{
    [JsonPropertyName("originalAmount")]
    public decimal OriginalAmount { get; set; }

    [JsonPropertyName("convertedAtMarketRate")]
    public decimal ConvertedAtMarketRate { get; set; }

    [JsonPropertyName("convertedAtBankRate")]
    public decimal ConvertedAtBankRate { get; set; }

    [JsonPropertyName("transferFee")]
    public decimal TransferFee { get; set; }

    [JsonPropertyName("platformFee")]
    public decimal PlatformFee { get; set; }

    [JsonPropertyName("gstOrTax")]
    public decimal GstOrTax { get; set; }

    [JsonPropertyName("totalDeductions")]
    public decimal TotalDeductions { get; set; }

    [JsonPropertyName("amountReceivedAfterDeductions")]
    public decimal AmountReceivedAfterDeductions { get; set; }

    [JsonPropertyName("exchangeGainLoss")]
    public decimal ExchangeGainLoss { get; set; }
}

public sealed class CurrencyTransferCalculationResponse
{
    [JsonPropertyName("source")]
    public CurrencySymbolResponse Source { get; set; } = new();

    [JsonPropertyName("destination")]
    public CurrencyDestinationResponse Destination { get; set; } = new();

    [JsonPropertyName("marketRate")]
    public decimal MarketRate { get; set; }

    [JsonPropertyName("bankRate")]
    public decimal BankRate { get; set; }

    [JsonPropertyName("grossAmountInINR")]
    public decimal GrossAmountInINR { get; set; }

    [JsonPropertyName("fees")]
    public CurrencyFeesResponse Fees { get; set; } = new();

    [JsonPropertyName("exchangeLoss")]
    public decimal ExchangeLoss { get; set; }

    [JsonPropertyName("netAmountReceived")]
    public decimal NetAmountReceived { get; set; }

    [JsonPropertyName("summary")]
    public CurrencySummaryResponse Summary { get; set; } = new();

    [JsonPropertyName("breakdown")]
    public CurrencyBreakdownResponse Breakdown { get; set; } = new();
}

public sealed class CurrencyTransferValidationResponse
{
    [JsonPropertyName("valid")]
    public bool Valid { get; set; }

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();
}