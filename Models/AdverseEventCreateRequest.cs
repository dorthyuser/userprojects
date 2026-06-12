using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace azurefunctionaeproject.Models;

public sealed class AdverseEventCreateRequest
{
    [Required]
    [JsonPropertyName("trialId")]
    public string TrialId { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("siteId")]
    public string SiteId { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("clinicianId")]
    public string ClinicianId { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("eventDate")]
    public DateTime EventDate { get; set; }

    [Required]
    [JsonPropertyName("aeTermCode")]
    public string AeTermCode { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("aeTermName")]
    public string AeTermName { get; set; } = string.Empty;

    [Range(1, 5)]
    [JsonPropertyName("ctcaeGrade")]
    public int CtcaeGrade { get; set; }

    [Required]
    [JsonPropertyName("serious")]
    public bool Serious { get; set; }

    [Required]
    [JsonPropertyName("outcome")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OutcomeEnum Outcome { get; set; }

    [Required]
    [JsonPropertyName("actionTaken")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ActionTakenEnum ActionTaken { get; set; }

    [Required]
    [StringLength(2000)]
    [JsonPropertyName("narrative")]
    public string Narrative { get; set; } = string.Empty;

    [JsonPropertyName("relatedDrugId")]
    public string? RelatedDrugId { get; set; }

    [Required]
    [EmailAddress]
    [JsonPropertyName("reportedBy")]
    public string ReportedBy { get; set; } = string.Empty;
}
