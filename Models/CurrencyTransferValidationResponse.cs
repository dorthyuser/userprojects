using System.Text.Json.Serialization;

namespace currency_calculator.Models;

public sealed class CurrencyTransferValidationResponse
{
    [JsonPropertyName("valid")]
    public bool Valid { get; set; }

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();
}