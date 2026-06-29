using System.Text.Json.Serialization;

namespace currency_calculator.Models;

public sealed class SupportedCurrencyResponse
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
    public List<SupportedCurrencyResponse> SupportedCurrencies { get; set; } = new();
}

public sealed class SupportedCurrenciesListResponse
{
    [JsonPropertyName("currencies")]
    public List<SupportedCurrencyResponse> Currencies { get; set; } = new();
}

public sealed class CurrencyTransferHealthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("service")]
    public string Service { get; set; } = string.Empty;

    [JsonPropertyName("dependencies")]
    public CurrencyTransferDependenciesResponse Dependencies { get; set; } = new();

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
}

public sealed class CurrencyTransferDependenciesResponse
{
    [JsonPropertyName("exchangeRateProvider")]
    public string ExchangeRateProvider { get; set; } = string.Empty;

    [JsonPropertyName("cache")]
    public string Cache { get; set; } = string.Empty;
}

public sealed class CurrencyRatesResponse
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
    public DateTimeOffset Timestamp { get; set; }
}

public sealed class CurrencyTransferValidationResponse
{
    [JsonPropertyName("valid")]
    public bool Valid { get; set; }

    [JsonPropertyName("errors")]
    public Dictionary<string, string> Errors { get; set; } = new();

    [JsonPropertyName("warnings")]
    public Dictionary<string, string> Warnings { get; set; } = new();
}

public sealed class CurrencyTransferCalculationResponse
{
    [JsonPropertyName("source")]
    public CurrencyTransferSourceResponse Source { get; set; } = new();

    [JsonPropertyName("destination")]
    public CurrencyTransferDestinationResponse Destination { get; set; } = new();

    [JsonPropertyName("marketRate")]
    public decimal MarketRate { get; set; }

    [JsonPropertyName("bankRate")]
    public decimal BankRate { get; set; }

    [JsonPropertyName("grossAmountInINR")]
    public decimal GrossAmountInINR { get; set; }

    [JsonPropertyName("fees")]
    public CurrencyTransferFeesResponse Fees { get; set; } = new();

    [JsonPropertyName("exchangeLoss")]
    public decimal ExchangeLoss { get; set; }

    [JsonPropertyName("netAmountReceived")]
    public decimal NetAmountReceived { get; set; }

    [JsonPropertyName("summary")]
    public CurrencyTransferSummaryResponse Summary { get; set; } = new();
}

public sealed class CurrencyTransferSourceResponse
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
}

public sealed class CurrencyTransferDestinationResponse
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;
}

public sealed class CurrencyTransferFeesResponse
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

public sealed class CurrencyTransferSummaryResponse
{
    [JsonPropertyName("senderPays")]
    public string SenderPays { get; set; } = string.Empty;

    [JsonPropertyName("receiverGets")]
    public string ReceiverGets { get; set; } = string.Empty;
}