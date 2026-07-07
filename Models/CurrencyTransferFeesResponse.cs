using System.Text.Json.Serialization;

namespace currency_calculator.Models;

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