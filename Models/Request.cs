using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Buyandsellgold1013Lambda.Models;

public sealed class CreateBuyOrderRequest
{
    [JsonPropertyName("customerId")]
    [Required]
    [MinLength(1)]
    [MaxLength(64)]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    [Required]
    [MinLength(1)]
    public List<CreateBuyOrderItemRequest> Items { get; set; } = new();

    [JsonPropertyName("deliveryAddress")]
    [Required]
    [MinLength(1)]
    [MaxLength(500)]
    public string DeliveryAddress { get; set; } = string.Empty;
}

public sealed class CreateBuyOrderItemRequest
{
    [JsonPropertyName("productId")]
    [Required]
    [MinLength(1)]
    [MaxLength(64)]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    [Required]
    public int Quantity { get; set; }
}

public sealed class CreateSellRequest
{
    [JsonPropertyName("customerId")]
    [Required]
    [MinLength(1)]
    [MaxLength(64)]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("productName")]
    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("goldPurity")]
    [Required]
    [MinLength(1)]
    [MaxLength(10)]
    public string GoldPurity { get; set; } = string.Empty;

    [JsonPropertyName("weightGrams")]
    [Required]
    public decimal WeightGrams { get; set; }

    [JsonPropertyName("expectedPrice")]
    public decimal? ExpectedPrice { get; set; }

    [JsonPropertyName("notes")]
    [MaxLength(1000)]
    public string? Notes { get; set; }
}