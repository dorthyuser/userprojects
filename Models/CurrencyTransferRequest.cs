using System.ComponentModel.DataAnnotations;

namespace currency_calculator.Models;

public sealed class CurrencyTransferRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(3)]
    public string FromCurrency { get; set; } = string.Empty;

    [Required]
    [MinLength(3)]
    [MaxLength(3)]
    public string ToCurrency { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; set; }
}