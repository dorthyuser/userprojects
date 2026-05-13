using System.Text.Json.Serialization;

namespace test_capi_1111123323333.Models;

public sealed class OrderResponse
{
    [JsonPropertyName("orderId")]
    public Guid OrderId { get; set; }

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("orderDate")]
    public DateTimeOffset OrderDate { get; set; }
}