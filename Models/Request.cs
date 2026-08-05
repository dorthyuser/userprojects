using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Csharpae1039Lambda.Models;

public sealed class AdverseEventRequest
{
    [Required]
    [JsonPropertyName("trialId")]
    [MaxLength(50)]
    public string? TrialId { get; set; }

    [Required]
    [JsonPropertyName("siteId")]
    [MaxLength(50)]
    public string? SiteId { get; set; }

    [Required]
    [JsonPropertyName("patientId")]
    [MaxLength(50)]
    public string? PatientId { get; set; }

    [Required]
    [JsonPropertyName("clinicianId")]
    [MaxLength(50)]
    public string? ClinicianId { get; set; }

    [Required]
    [JsonPropertyName("eventDate")]
    public DateTime? EventDate { get; set; }

    [Required]
    [JsonPropertyName("aeTermCode")]
    [MaxLength(20)]
    public string? AeTermCode { get; set; }

    [Required]
    [JsonPropertyName("aeTermName")]
    [MaxLength(255)]
    public string? AeTermName { get; set; }

    [Required]
    [JsonPropertyName("ctcaeGrade")]
    public int? CtcaeGrade { get; set; }

    [Required]
    [JsonPropertyName("serious")]
    public bool? Serious { get; set; }

    [Required]
    [JsonPropertyName("outcome")]
    public OutcomeEnum? Outcome { get; set; }

    [Required]
    [JsonPropertyName("actionTaken")]
    public ActionTakenEnum? ActionTaken { get; set; }

    [Required]
    [JsonPropertyName("narrative")]
    [MaxLength(2000)]
    public string? Narrative { get; set; }

    [JsonPropertyName("relatedDrugId")]
    [MaxLength(50)]
    public string? RelatedDrugId { get; set; }

    [Required]
    [JsonPropertyName("reportedBy")]
    [MaxLength(255)]
    public string? ReportedBy { get; set; }
}

public sealed class NotificationQueryRequest
{
    [JsonPropertyName("trialId")]
    [MaxLength(50)]
    public string? TrialId { get; set; }

    [JsonPropertyName("siteId")]
    [MaxLength(50)]
    public string? SiteId { get; set; }

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

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int? PageSize { get; set; }
}