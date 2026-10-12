namespace AdverseEventNotificationService.Api.Domain;

public sealed record AdverseEventNotificationEntity(
    long Id,
    string NotificationId,
    string AeId,
    string TrialId,
    string SiteId,
    string PatientId,
    string AeTermName,
    int CtcaeGrade,
    bool Serious,
    AdverseEventOutcome Outcome,
    AdverseEventPriority Priority,
    bool Acknowledged,
    string? AcknowledgedBy,
    DateTimeOffset? AcknowledgedAt,
    bool SnsPublished,
    string? SnsMessageId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);