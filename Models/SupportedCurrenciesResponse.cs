using System.Text.Json.Serialization;

namespace currency_calculator.Models;

public sealed class SupportedCurrenciesResponse
{
    [JsonPropertyName("supportedCurrencies")]
    public List<SupportedCurrencyResponse> SupportedCurrencies { get; set; } = new();
}