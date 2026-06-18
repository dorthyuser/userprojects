using System.Text.Json.Serialization;

namespace csharpapi248pm.Models;

public sealed class AdverseEventNotificationsResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("notifications")]
    public List<AdverseEventNotificationItemResponse> Notifications { get; set; } = new();
}