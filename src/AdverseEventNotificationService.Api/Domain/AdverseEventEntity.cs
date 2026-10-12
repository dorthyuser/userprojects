namespace AdverseEventNotificationService.Api.Domain;

public sealed record AdverseEventEntity(
    long Id,
    string AeId,
    string TrialId,
    string SiteId,
    string PatientId,
    string ClinicianId,
    DateTimeOffset EventDate,
    string AeTermCode,
    string AeTermName,
    int CtcaeGrade,
    bool Serious,
    AdverseEventOutcome Outcome,
    ActionTaken ActionTaken,
    string Narrative,
    string? RelatedDrugId,
    string ReportedBy,
    DateTimeOffset SubmittedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);