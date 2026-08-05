using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Csharpae1140Lambda.Models;

public sealed class AdverseEventRequest
{
    [Required]
    [JsonPropertyName("trialId")]
    public string? TrialId { get; set; }

    [Required]
    [JsonPropertyName("siteId")]
    public string? SiteId { get; set; }

    [Required]
    [JsonPropertyName("patientId")]
    public string? PatientId { get; set; }

    [Required]
    [JsonPropertyName("clinicianId")]
    public string? ClinicianId { get; set; }

    [Required]
    [JsonPropertyName("eventDate")]
    public DateTime? EventDate { get; set; }

    [Required]
    [JsonPropertyName("aeTermCode")]
    public string? AeTermCode { get; set; }

    [Required]
    [JsonPropertyName("aeTermName")]
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
    [MaxLength(2000)]
    [JsonPropertyName("narrative")]
    public string? Narrative { get; set; }

    [JsonPropertyName("relatedDrugId")]
    public string? RelatedDrugId { get; set; }

    [Required]
    [JsonPropertyName("reportedBy")]
    public string? ReportedBy { get; set; }
}