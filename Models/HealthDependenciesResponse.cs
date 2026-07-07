using System.Text.Json.Serialization;

namespace currency_calculator.Models;

public sealed class HealthDependenciesResponse
{
    [JsonPropertyName("exchangeRateProvider")]
    public string ExchangeRateProvider { get; set; } = string.Empty;

    [JsonPropertyName("cache")]
    public string Cache { get; set; } = string.Empty;
}