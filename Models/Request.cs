using System.Text.Json.Serialization;

namespace Aelambda1024Lambda.Models;

public sealed class AdverseEventRequest
{
    public string? TrialId { get; set; }
    public string? SiteId { get; set; }
    public string? PatientId { get; set; }
    public string? ClinicianId { get; set; }
    public DateTimeOffset? EventDate { get; set; }
    public string? AeTermCode { get; set; }
    public string? AeTermName { get; set; }
    public int? CtcaeGrade { get; set; }
    public bool? Serious { get; set; }
    public AeOutcome? Outcome { get; set; }
    public AeActionTaken? ActionTaken { get; set; }
    public string? Narrative { get; set; }
    public string? RelatedDrugId { get; set; }
    public string? ReportedBy { get; set; }
}

public sealed class NotificationQuery
{
    public string? TrialId { get; set; }
    public string? SiteId { get; set; }
    public int? CtcaeGrade { get; set; }
    public bool? Serious { get; set; }
    public bool? Acknowledged { get; set; }
    public NotificationPriority? Priority { get; set; }
    public DateTimeOffset? DateFrom { get; set; }
    public DateTimeOffset? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}