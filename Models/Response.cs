using System.Text.Json.Serialization;

namespace Buyandsellgold1013Lambda.Models;

public sealed class ProductResponse
{
    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("goldPurity")]
    public string GoldPurity { get; set; } = string.Empty;

    [JsonPropertyName("weightGrams")]
    public decimal WeightGrams { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public sealed class PaginationResponse
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalRecords")]
    public int TotalRecords { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }
}

public sealed class ProductListResponse
{
    [JsonPropertyName("data")]
    public List<ProductResponse> Data { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationResponse Pagination { get; set; } = new();
}

public sealed class BuyOrderItemResponse
{
    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }
}

public sealed class BuyOrderResponse
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<BuyOrderItemResponse> Items { get; set; } = new();

    [JsonPropertyName("deliveryAddress")]
    public string DeliveryAddress { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CreateBuyOrderResponse
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class SellRequestResponse
{
    [JsonPropertyName("sellRequestId")]
    public string SellRequestId { get; set; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("goldPurity")]
    public string GoldPurity { get; set; } = string.Empty;

    [JsonPropertyName("weightGrams")]
    public decimal WeightGrams { get; set; }

    [JsonPropertyName("expectedPrice")]
    public decimal? ExpectedPrice { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CreateSellRequestResponse
{
    [JsonPropertyName("sellRequestId")]
    public string SellRequestId { get; set; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ErrorResponse
{
    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; set; } = string.Empty;

    [JsonPropertyName("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;
}