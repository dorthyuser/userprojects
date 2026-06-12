using System;
using System.Text.Json.Serialization;

namespace azurefunctionaeproject.Models;

public sealed class AdverseEventCreateResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "success";

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
