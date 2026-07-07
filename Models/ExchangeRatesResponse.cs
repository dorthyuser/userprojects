using System.Text.Json.Serialization;

namespace currency_calculator.Models;

public sealed class ExchangeRatesResponse
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