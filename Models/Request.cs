namespace Csharpae1039Lambda;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public sealed class AdverseEventRequest
{
    [Required]
    [MaxLength(50)]
    [JsonPropertyName("trialId")]
    public string? TrialId { get; set; }

    [Required]
    [MaxLength(50)]
    [JsonPropertyName("siteId")]
    public string? SiteId { get; set; }

    [Required]
    [MaxLength(50)]
    [JsonPropertyName("patientId")]
    public string? PatientId { get; set; }

    [Required]
    [MaxLength(50)]
    [JsonPropertyName("clinicianId")]
    public string? ClinicianId { get; set; }

    [Required]
    [JsonPropertyName("eventDate")]
    public DateTimeOffset? EventDate { get; set; }

    [Required]
    [MaxLength(20)]
    [JsonPropertyName("aeTermCode")]
    public string? AeTermCode { get; set; }

    [Required]
    [MaxLength(255)]
    [JsonPropertyName("aeTermName")]
    public string? AeTermName { get; set; }

    [Required]
    [Range(1, 5)]
    [JsonPropertyName("ctcaeGrade")]
    public int? CtcaeGrade { get; set; }

    [Required]
    [JsonPropertyName("serious")]
    public bool? Serious { get; set; }

    [Required]
    [JsonPropertyName("outcome")]
    public AeOutcome? Outcome { get; set; }

    [Required]
    [JsonPropertyName("actionTaken")]
    public AeActionTaken? ActionTaken { get; set; }

    [Required]
    [MaxLength(2000)]
    [JsonPropertyName("narrative")]
    public string? Narrative { get; set; }

    [MaxLength(50)]
    [JsonPropertyName("relatedDrugId")]
    public string? RelatedDrugId { get; set; }

    [Required]
    [MaxLength(255)]
    [JsonPropertyName("reportedBy")]
    public string? ReportedBy { get; set; }
}