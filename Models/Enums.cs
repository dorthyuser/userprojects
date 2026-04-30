using System.Text.Json.Serialization;

namespace Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OrderStatus
    {
        pending,
        processing,
        shipped,
        delivered,
        cancelled
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentStatus
    {
        pending,
        paid,
        failed,
        refunded
    }
}