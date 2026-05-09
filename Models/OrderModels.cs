using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace jkjkjkjkjkjkjkjkjkjkjkjkjkjjk.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OrderStatusEnum
    {
        Pending,
        Confirmed,
        Cancelled
    }

    public sealed class CreateOrderRequest
    {
        [Required]
        [MinLength(1)]
        [MaxLength(128)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        [MaxLength(64)]
        public string ProductCode { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }
    }

    public sealed class UpdateOrderRequest
    {
        [Required]
        [MinLength(1)]
        [MaxLength(128)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        [MaxLength(64)]
        public string ProductCode { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }
    }

    public sealed class OrderResponse
    {
        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("customerName")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName("productCode")]
        public string ProductCode { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("status")]
        public OrderStatusEnum Status { get; set; }
    }

    public sealed class CancelOrderResponse
    {
        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("cancelled")]
        public bool Cancelled { get; set; }
    }
}