using System.Text.Json.Serialization;

namespace csharpapi248pm.Models;

public sealed class AdverseEventNotificationItemResponse
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
    public PriorityEnum Priority { get; set; }

    [JsonPropertyName("outcome")]
    public OutcomeEnum Outcome { get; set; }

    [JsonPropertyName("acknowledged")]
    public bool Acknowledged { get; set; }

    [JsonPropertyName("snsPublished")]
    public bool SnsPublished { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}