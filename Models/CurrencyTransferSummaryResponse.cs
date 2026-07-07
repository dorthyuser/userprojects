using System.Text.Json.Serialization;

namespace currency_calculator.Models;

public sealed class CurrencyTransferSummaryResponse
{
    [JsonPropertyName("senderPays")]
    public string SenderPays { get; set; } = string.Empty;

    [JsonPropertyName("receiverGets")]
    public string ReceiverGets { get; set; } = string.Empty;
}