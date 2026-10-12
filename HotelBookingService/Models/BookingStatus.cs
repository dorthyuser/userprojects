using System.Text.Json.Serialization;

namespace HotelBookingService.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BookingStatus
{
    Confirmed,
    Cancelled
}
