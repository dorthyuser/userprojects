using System.Text.Json.Serialization;

namespace HotelBookingService.Models;

public sealed class CreateBookingResponse
{
    [JsonPropertyName("bookingId")]
    public string BookingId { get; set; } = string.Empty;

    [JsonPropertyName("bookingNumber")]
    public string BookingNumber { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public BookingStatus Status { get; set; }

    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("hotelId")]
    public string HotelId { get; set; } = string.Empty;

    [JsonPropertyName("roomType")]
    public string RoomType { get; set; } = string.Empty;

    [JsonPropertyName("checkInDate")]
    public DateOnly CheckInDate { get; set; }

    [JsonPropertyName("checkOutDate")]
    public DateOnly CheckOutDate { get; set; }

    [JsonPropertyName("guests")]
    public int Guests { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class GetBookingResponse
{
    [JsonPropertyName("bookingId")]
    public string BookingId { get; set; } = string.Empty;

    [JsonPropertyName("bookingNumber")]
    public string BookingNumber { get; set; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("hotelId")]
    public string HotelId { get; set; } = string.Empty;

    [JsonPropertyName("hotelName")]
    public string HotelName { get; set; } = string.Empty;

    [JsonPropertyName("roomType")]
    public string RoomType { get; set; } = string.Empty;

    [JsonPropertyName("checkInDate")]
    public DateOnly CheckInDate { get; set; }

    [JsonPropertyName("checkOutDate")]
    public DateOnly CheckOutDate { get; set; }

    [JsonPropertyName("guests")]
    public int Guests { get; set; }

    [JsonPropertyName("status")]
    public BookingStatus Status { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class CancelBookingResponse
{
    [JsonPropertyName("bookingId")]
    public string BookingId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public BookingStatus Status { get; set; }

    [JsonPropertyName("cancelReason")]
    public string? CancelReason { get; set; }

    [JsonPropertyName("cancelledAt")]
    public DateTimeOffset CancelledAt { get; set; }
}

public sealed class HotelAvailabilityResponse
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalRecords")]
    public int TotalRecords { get; set; }

    [JsonPropertyName("results")]
    public List<HotelAvailabilityResultResponse> Results { get; set; } = new();
}

public sealed class HotelAvailabilityResultResponse
{
    [JsonPropertyName("hotelId")]
    public string HotelId { get; set; } = string.Empty;

    [JsonPropertyName("hotelName")]
    public string HotelName { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("roomType")]
    public string RoomType { get; set; } = string.Empty;

    [JsonPropertyName("availableRooms")]
    public int AvailableRooms { get; set; }

    [JsonPropertyName("pricePerNight")]
    public decimal PricePerNight { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;
}
