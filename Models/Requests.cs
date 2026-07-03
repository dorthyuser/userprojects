using System.ComponentModel.DataAnnotations;

namespace currency_calculator.Models;

public sealed class CalculateRequest
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

public sealed class ValidateRequest
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

public sealed class RatesQueryRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(3)]
    public string BaseCurrency { get; set; } = string.Empty;

    [Required]
    [MinLength(3)]
    [MaxLength(3)]
    public string QuoteCurrency { get; set; } = string.Empty;
}