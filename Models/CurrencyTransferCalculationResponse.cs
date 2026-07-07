using System.Text.Json.Serialization;

namespace currency_calculator.Models;

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

    [JsonPropertyName("breakdown")]
    public CurrencyTransferBreakdownResponse Breakdown { get; set; } = new();
}