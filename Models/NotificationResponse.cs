using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace azurefunctionaeproject.Models;

public sealed class NotificationResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "success";

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("notifications")]
    public List<NotificationItem> Notifications { get; set; } = new();
}
