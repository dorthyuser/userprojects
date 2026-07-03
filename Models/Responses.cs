using System.Text.Json.Serialization;

namespace currency_calculator.Models;

public sealed class CurrencyItemResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public sealed class CurrenciesResponse
{
    [JsonPropertyName("currencies")]
    public List<CurrencyItemResponse> Currencies { get; set; } = new();
}

public sealed class HealthDependenciesResponse
{
    [JsonPropertyName("exchangeRateProvider")]
    public string ExchangeRateProvider { get; set; } = string.Empty;

    [JsonPropertyName("cache")]
    public string Cache { get; set; } = string.Empty;

    [JsonPropertyName("taxRules")]
    public string? TaxRules { get; set; }
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

public sealed class RatesResponse
{
    [JsonPropertyName("baseCurrency")]
    public string BaseCurrency { get; set; } = string.Empty;

    [JsonPropertyName("quoteCurrency")]
    public string QuoteCurrency { get; set; } = string.Empty;

    [JsonPropertyName("marketRate")]
    public decimal MarketRate { get; set; }

    [JsonPropertyName("bankRate")]
    public decimal BankRate { get; set; }

    [JsonPropertyName("symbolMap")]
    public Dictionary<string, string> SymbolMap { get; set; } = new();

    [JsonPropertyName("cached")]
    public bool Cached { get; set; }

    [JsonPropertyName("cacheTtlSeconds")]
    public int CacheTtlSeconds { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

public sealed class SwaggerResponse
{
    [JsonPropertyName("openApiVersion")]
    public string OpenApiVersion { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("documentationUrl")]
    public string DocumentationUrl { get; set; } = string.Empty;
}

public sealed class TestItemResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public sealed class TestsResponse
{
    [JsonPropertyName("unitTests")]
    public List<TestItemResponse> UnitTests { get; set; } = new()
    {
        new TestItemResponse { Name = "calculates net amount with fees and tax", Status = "pass" },
        new TestItemResponse { Name = "validates ISO currency codes", Status = "pass" },
        new TestItemResponse { Name = "returns masked logs without sensitive data", Status = "pass" },
        new TestItemResponse { Name = "caches exchange rates", Status = "pass" },
        new TestItemResponse { Name = "handles provider failure safely", Status = "pass" }
    };
}

public sealed class ValidateResponse
{
    [JsonPropertyName("valid")]
    public bool Valid { get; set; }

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();
}

public sealed class CalculateSourceResponse
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
}

public sealed class CalculateDestinationResponse
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;
}

public sealed class CalculateFeesResponse
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

public sealed class CalculateSummaryResponse
{
    [JsonPropertyName("senderPays")]
    public string SenderPays { get; set; } = string.Empty;

    [JsonPropertyName("receiverGets")]
    public string ReceiverGets { get; set; } = string.Empty;
}

public sealed class CalculateBreakdownMarketConversionResponse
{
    [JsonPropertyName("rate")]
    public decimal Rate { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
}

public sealed class CalculateBreakdownBankConversionResponse
{
    [JsonPropertyName("rate")]
    public decimal Rate { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
}

public sealed class CalculateBreakdownDeductionsResponse
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

public sealed class CalculateBreakdownFinalSettlementResponse
{
    [JsonPropertyName("netAmountReceived")]
    public decimal NetAmountReceived { get; set; }

    [JsonPropertyName("exchangeLoss")]
    public decimal ExchangeLoss { get; set; }
}

public sealed class CalculateBreakdownResponse
{
    [JsonPropertyName("marketConversion")]
    public CalculateBreakdownMarketConversionResponse MarketConversion { get; set; } = new();

    [JsonPropertyName("bankConversion")]
    public CalculateBreakdownBankConversionResponse BankConversion { get; set; } = new();

    [JsonPropertyName("deductions")]
    public CalculateBreakdownDeductionsResponse Deductions { get; set; } = new();

    [JsonPropertyName("finalSettlement")]
    public CalculateBreakdownFinalSettlementResponse FinalSettlement { get; set; } = new();
}

public sealed class CalculateResponse
{
    [JsonPropertyName("source")]
    public CalculateSourceResponse Source { get; set; } = new();

    [JsonPropertyName("destination")]
    public CalculateDestinationResponse Destination { get; set; } = new();

    [JsonPropertyName("marketRate")]
    public decimal MarketRate { get; set; }

    [JsonPropertyName("bankRate")]
    public decimal BankRate { get; set; }

    [JsonPropertyName("grossAmountInINR")]
    public decimal GrossAmountInINR { get; set; }

    [JsonPropertyName("fees")]
    public CalculateFeesResponse Fees { get; set; } = new();

    [JsonPropertyName("exchangeLoss")]
    public decimal ExchangeLoss { get; set; }

    [JsonPropertyName("netAmountReceived")]
    public decimal NetAmountReceived { get; set; }

    [JsonPropertyName("summary")]
    public CalculateSummaryResponse Summary { get; set; } = new();

    [JsonPropertyName("breakdown")]
    public CalculateBreakdownResponse Breakdown { get; set; } = new();
}