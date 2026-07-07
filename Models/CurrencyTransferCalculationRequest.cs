using System.ComponentModel.DataAnnotations;

namespace currency_calculator.Models;

public sealed class CurrencyTransferCalculationRequest
{
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string FromCurrency { get; set; } = string.Empty;

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string ToCurrency { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999999.99")]
    public decimal Amount { get; set; }
}