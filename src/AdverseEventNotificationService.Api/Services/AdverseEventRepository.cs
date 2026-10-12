using AdverseEventNotificationService.Api.Domain;
using Dapper;

namespace AdverseEventNotificationService.Api.Services;

public sealed class AdverseEventRepository : IAdverseEventRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AdverseEventRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> TrialExistsAsync(string trialId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var result = await connection.ExecuteScalarAsync<int?>(new CommandDefinition("SELECT 1 FROM trials WHERE trial_id = @TrialId AND status = 'ACTIVE' LIMIT 1", new { TrialId = trialId }, cancellationToken: cancellationToken));
        return result.HasValue;
    }

    public async Task<bool> PatientEnrolledAsync(string trialId, string patientId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var result = await connection.ExecuteScalarAsync<int?>(new CommandDefinition("SELECT 1 FROM trial_enrolments WHERE trial_id = @TrialId AND patient_id = @PatientId AND status = 'ENROLLED' LIMIT 1", new { TrialId = trialId, PatientId = patientId }, cancellationToken: cancellationToken));
        return result.HasValue;
    }

    public async Task<string?> FindDuplicateAeIdAsync(string trialId, string patientId, string aeTermCode, int ctcaeGrade, DateTimeOffset windowStart, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<string?>(new CommandDefinition("SELECT ae_id FROM adverse_events WHERE trial_id = @TrialId AND patient_id = @PatientId AND ae_term_code = @AeTermCode AND ctcae_grade = @CtcaeGrade AND submitted_at >= @WindowStart ORDER BY submitted_at DESC LIMIT 1", new { TrialId = trialId, PatientId = patientId, AeTermCode = aeTermCode, CtcaeGrade = ctcaeGrade, WindowStart = windowStart }, cancellationToken: cancellationToken));
    }

    public async Task InsertAsync(AdverseEventEntity entity, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (@AeId, @TrialId, @SiteId, @PatientId, @ClinicianId, @EventDate, @AeTermCode, @AeTermName, @CtcaeGrade, @Serious, @Outcome, @ActionTaken, @Narrative, @RelatedDrugId, @ReportedBy, @SubmittedAt, @CreatedAt, @UpdatedAt)";
        await connection.ExecuteAsync(new CommandDefinition(sql, entity, cancellationToken: cancellationToken));
    }
}