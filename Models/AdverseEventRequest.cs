using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace csharpapi248pm.Models;

public sealed class AdverseEventRequest
{
    [Required, MaxLength(50)]
    [JsonPropertyName("trialId")]
    public string TrialId { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    [JsonPropertyName("siteId")]
    public string SiteId { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    [JsonPropertyName("clinicianId")]
    public string ClinicianId { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("eventDate")]
    public DateTime EventDate { get; set; }

    [Required, MaxLength(20)]
    [JsonPropertyName("aeTermCode")]
    public string AeTermCode { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    [JsonPropertyName("aeTermName")]
    public string AeTermName { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("ctcaeGrade")]
    public int CtcaeGrade { get; set; }

    [Required]
    [JsonPropertyName("serious")]
    public bool Serious { get; set; }

    [Required]
    [JsonPropertyName("outcome")]
    public OutcomeEnum Outcome { get; set; }

    [Required]
    [JsonPropertyName("actionTaken")]
    public ActionTakenEnum ActionTaken { get; set; }

    [Required, MaxLength(2000)]
    [JsonPropertyName("narrative")]
    public string Narrative { get; set; } = string.Empty;

    [MaxLength(50)]
    [JsonPropertyName("relatedDrugId")]
    public string? RelatedDrugId { get; set; }

    [Required, MaxLength(255)]
    [JsonPropertyName("reportedBy")]
    public string ReportedBy { get; set; } = string.Empty;
}