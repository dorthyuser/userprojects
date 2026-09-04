using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Buyandsellgold1013Lambda.Models;

public sealed class CreateBuyOrderRequest
{
    [Required]
    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [JsonPropertyName("items")]
    public List<CreateBuyOrderItemRequest> Items { get; set; } = new();

    [Required]
    [MinLength(1)]
    [JsonPropertyName("deliveryAddress")]
    public string DeliveryAddress { get; set; } = string.Empty;
}

public sealed class CreateBuyOrderItemRequest
{
    [Required]
    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}

public sealed class CreateSellRequestRequest
{
    [Required]
    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [JsonPropertyName("goldPurity")]
    public string GoldPurity { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.001", "79228162514264337593543950335")]
    [JsonPropertyName("weightGrams")]
    public decimal WeightGrams { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    [JsonPropertyName("expectedPrice")]
    public decimal? ExpectedPrice { get; set; }

    [MaxLength(1000)]
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}