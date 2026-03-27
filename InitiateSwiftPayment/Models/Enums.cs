using System.Text.Json.Serialization;

namespace InitiateSwiftPayment.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BankSwiftPaymentType
    {
        MT103,
        MT202,
        pacs_008,
        pacs_009
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BankPaymentStatus
    {
        PENDING,
        ACCEPTED,
        SCREENING_CLEAR,
        CBS_POSTED,
        SWIFT_SENT,
        COMPLETED,
        FAILED,
        CANCELLED
    }
}
