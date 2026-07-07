using System.Text.Json.Serialization;

namespace currency_calculator.Models;

public sealed class CurrencyTransferBreakdownResponse
{
    [JsonPropertyName("originalAmount")]
    public decimal OriginalAmount { get; set; }

    [JsonPropertyName("convertedAtMarketRate")]
    public decimal ConvertedAtMarketRate { get; set; }

    [JsonPropertyName("convertedAtBankRate")]
    public decimal ConvertedAtBankRate { get; set; }

    [JsonPropertyName("transferFee")]
    public decimal TransferFee { get; set; }

    [JsonPropertyName("platformFee")]
    public decimal PlatformFee { get; set; }

    [JsonPropertyName("gstOrTax")]
    public decimal GstOrTax { get; set; }

    [JsonPropertyName("totalDeductions")]
    public decimal TotalDeductions { get; set; }

    [JsonPropertyName("amountReceivedAfterDeductions")]
    public decimal AmountReceivedAfterDeductions { get; set; }

    [JsonPropertyName("exchangeGainLoss")]
    public decimal ExchangeGainLoss { get; set; }
}