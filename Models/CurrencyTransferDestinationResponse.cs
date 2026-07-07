using System.Text.Json.Serialization;

namespace currency_calculator.Models;

public sealed class CurrencyTransferDestinationResponse
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;
}