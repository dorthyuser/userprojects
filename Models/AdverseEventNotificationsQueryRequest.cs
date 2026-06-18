using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace csharpapi248pm.Models;

public sealed class AdverseEventNotificationsQueryRequest
{
    [MaxLength(50)]
    [JsonPropertyName("trialId")]
    public string? TrialId { get; set; }

    [MaxLength(50)]
    [JsonPropertyName("siteId")]
    public string? SiteId { get; set; }

    [Range(1, 5)]
    [JsonPropertyName("ctcaeGrade")]
    public int? CtcaeGrade { get; set; }

    [JsonPropertyName("serious")]
    public bool? Serious { get; set; }

    [JsonPropertyName("acknowledged")]
    public bool? Acknowledged { get; set; }

    [JsonPropertyName("priority")]
    public PriorityEnum? Priority { get; set; }

    [JsonPropertyName("dateFrom")]
    public DateTime? DateFrom { get; set; }

    [JsonPropertyName("dateTo")]
    public DateTime? DateTo { get; set; }

    [Range(1, int.MaxValue)]
    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [Range(1, 100)]
    [JsonPropertyName("pageSize")]
    public int? PageSize { get; set; }
}