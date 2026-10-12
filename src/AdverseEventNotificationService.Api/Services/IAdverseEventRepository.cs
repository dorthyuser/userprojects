using AdverseEventNotificationService.Api.Domain;

namespace AdverseEventNotificationService.Api.Services;

public interface IAdverseEventRepository
{
    Task<bool> TrialExistsAsync(string trialId, CancellationToken cancellationToken);
    Task<bool> PatientEnrolledAsync(string trialId, string patientId, CancellationToken cancellationToken);
    Task<string?> FindDuplicateAeIdAsync(string trialId, string patientId, string aeTermCode, int ctcaeGrade, DateTimeOffset windowStart, CancellationToken cancellationToken);
    Task InsertAsync(AdverseEventEntity entity, CancellationToken cancellationToken);
}