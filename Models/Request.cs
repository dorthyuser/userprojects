namespace Csharpae1012Lambda.Models;

using Csharpae1012Lambda;

public sealed class Request
{
    public string TrialId { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string ClinicianId { get; set; } = string.Empty;
    public string EventDate { get; set; } = string.Empty;
    public string AeTermCode { get; set; } = string.Empty;
    public string AeTermName { get; set; } = string.Empty;
    public int CtcaeGrade { get; set; }
    public bool Serious { get; set; }
    public Outcome Outcome { get; set; }
    public ActionTaken ActionTaken { get; set; }
    public string Narrative { get; set; } = string.Empty;
    public string? RelatedDrugId { get; set; }
    public string ReportedBy { get; set; } = string.Empty;
}