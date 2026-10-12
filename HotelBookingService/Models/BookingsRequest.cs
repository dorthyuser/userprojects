using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HotelBookingService.Models;

public sealed class CreateBookingRequest
{
    [Required]
    [MaxLength(20)]
    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [JsonPropertyName("hotelId")]
    public string HotelId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [JsonPropertyName("roomType")]
    public string RoomType { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("checkInDate")]
    public DateOnly CheckInDate { get; set; }

    [Required]
    [JsonPropertyName("checkOutDate")]
    public DateOnly CheckOutDate { get; set; }

    [Range(1, int.MaxValue)]
    [JsonPropertyName("guests")]
    public int Guests { get; set; }

    [Required]
    [MaxLength(3)]
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [JsonPropertyName("idempotencyKey")]
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class CancelBookingRequest
{
    [MaxLength(250)]
    [JsonPropertyName("cancelReason")]
    public string? CancelReason { get; set; }
}

public sealed class HotelAvailabilityRequest
{
    [MaxLength(20)]
    [JsonPropertyName("hotelId")]
    public string? HotelId { get; set; }

    [MaxLength(100)]
    [JsonPropertyName("city")]
    public string? City { get; set; }

    [Required]
    [JsonPropertyName("checkInDate")]
    public DateOnly CheckInDate { get; set; }

    [Required]
    [JsonPropertyName("checkOutDate")]
    public DateOnly CheckOutDate { get; set; }

    [Range(1, int.MaxValue)]
    [JsonPropertyName("guests")]
    public int Guests { get; set; }

    [MaxLength(50)]
    [JsonPropertyName("roomType")]
    public string? RoomType { get; set; }

    [Range(1, int.MaxValue)]
    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 20;
}
