using System.Text.Json.Serialization;

namespace csharpapi248pm.Models;

public sealed class AdverseEventSubmitResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("aeId")]
    public string AeId { get; set; } = string.Empty;

    [JsonPropertyName("notificationId")]
    public string NotificationId { get; set; } = string.Empty;

    [JsonPropertyName("snsPublished")]
    public bool SnsPublished { get; set; }

    [JsonPropertyName("snsMessageId")]
    public string? SnsMessageId { get; set; }

    [JsonPropertyName("receivedAt")]
    public DateTime ReceivedAt { get; set; }
}