using System.ComponentModel.DataAnnotations;
using AdverseEventNotificationService.Api.Domain;

namespace AdverseEventNotificationService.Api.Contracts;

public sealed record AdverseEventRequestDto
{
    [Required]
    public string TrialId { get; init; } = string.Empty;

    [Required]
    public string SiteId { get; init; } = string.Empty;

    [Required]
    public string PatientId { get; init; } = string.Empty;

    [Required]
    public string ClinicianId { get; init; } = string.Empty;

    [Required]
    public DateTimeOffset EventDate { get; init; }

    [Required]
    public string AeTermCode { get; init; } = string.Empty;

    [Required]
    public string AeTermName { get; init; } = string.Empty;

    [Range(1, 5)]
    public int CtcaeGrade { get; init; }

    [Required]
    public bool Serious { get; init; }

    [Required]
    public AdverseEventOutcome Outcome { get; init; }

    [Required]
    public ActionTaken ActionTaken { get; init; }

    [Required]
    [MaxLength(2000)]
    public string Narrative { get; init; } = string.Empty;

    public string? RelatedDrugId { get; init; }

    [Required]
    [EmailAddress]
    public string ReportedBy { get; init; } = string.Empty;
}