namespace Csharpae1039Lambda;

using System.Text.Json.Serialization;

public sealed class PostAdverseEventResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("aeId")]
    public string AeId { get; set; } = string.Empty;

    [JsonPropertyName("notificationId")]
    public string NotificationId { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("receivedAt")]
    public DateTimeOffset ReceivedAt { get; set; }
}

public sealed class NotificationListResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("total")]
    public long Total { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("notifications")]
    public List<NotificationResponseItem> Notifications { get; set; } = new();
}

public sealed class NotificationResponseItem
{
    [JsonPropertyName("notificationId")]
    public string NotificationId { get; set; } = string.Empty;

    [JsonPropertyName("aeId")]
    public string AeId { get; set; } = string.Empty;

    [JsonPropertyName("trialId")]
    public string TrialId { get; set; } = string.Empty;

    [JsonPropertyName("siteId")]
    public string SiteId { get; set; } = string.Empty;

    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [JsonPropertyName("aeTermName")]
    public string AeTermName { get; set; } = string.Empty;

    [JsonPropertyName("ctcaeGrade")]
    public int CtcaeGrade { get; set; }

    [JsonPropertyName("serious")]
    public bool Serious { get; set; }

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty;

    [JsonPropertyName("outcome")]
    public string Outcome { get; set; } = string.Empty;

    [JsonPropertyName("acknowledged")]
    public bool Acknowledged { get; set; }

    [JsonPropertyName("acknowledgedBy")]
    public string? AcknowledgedBy { get; set; }

    [JsonPropertyName("acknowledgedAt")]
    public DateTimeOffset? AcknowledgedAt { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }
}