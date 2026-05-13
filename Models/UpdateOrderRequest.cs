using System.ComponentModel.DataAnnotations;

namespace test_capi_1111123323333.Models;

public sealed class UpdateOrderRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    public DateTimeOffset OrderDate { get; set; }
}