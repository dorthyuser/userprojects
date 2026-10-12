using AdverseEventNotificationService.Api.Contracts;
using AdverseEventNotificationService.Api.Domain;
using Dapper;

namespace AdverseEventNotificationService.Api.Services;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public NotificationRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InsertAsync(AdverseEventNotificationEntity entity, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, acknowledged_by, acknowledged_at, sns_published, sns_message_id, created_at, updated_at) VALUES (@NotificationId, @AeId, @TrialId, @SiteId, @PatientId, @AeTermName, @CtcaeGrade, @Serious, @Outcome, @Priority, @Acknowledged, @AcknowledgedBy, @AcknowledgedAt, @SnsPublished, @SnsMessageId, @CreatedAt, @UpdatedAt)";
        await connection.ExecuteAsync(new CommandDefinition(sql, entity, cancellationToken: cancellationToken));
    }

    public async Task<(long Total, IReadOnlyList<AdverseEventNotificationItemDto> Items)> SearchAsync(NotificationQueryDto query, int page, int pageSize, CancellationToken cancellationToken)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.TrialId)) { conditions.Add("trial_id = @TrialId"); parameters.Add("TrialId", query.TrialId); }
        if (!string.IsNullOrWhiteSpace(query.SiteId)) { conditions.Add("site_id = @SiteId"); parameters.Add("SiteId", query.SiteId); }
        if (query.CtcaeGrade.HasValue) { conditions.Add("ctcae_grade = @CtcaeGrade"); parameters.Add("CtcaeGrade", query.CtcaeGrade.Value); }
        if (query.Serious.HasValue) { conditions.Add("serious = @Serious"); parameters.Add("Serious", query.Serious.Value); }
        if (query.Acknowledged.HasValue) { conditions.Add("acknowledged = @Acknowledged"); parameters.Add("Acknowledged", query.Acknowledged.Value); }
        if (query.Priority.HasValue) { conditions.Add("priority = @Priority"); parameters.Add("Priority", query.Priority.Value.ToString().ToUpperInvariant()); }
        if (query.DateFrom.HasValue) { conditions.Add("created_at >= @DateFrom"); parameters.Add("DateFrom", query.DateFrom.Value); }
        if (query.DateTo.HasValue) { conditions.Add("created_at <= @DateTo"); parameters.Add("DateTo", query.DateTo.Value); }

        var whereClause = conditions.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", conditions);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(new CommandDefinition($"SELECT COUNT(*) FROM ae_notifications{whereClause}", parameters, cancellationToken: cancellationToken));
        parameters.Add("Limit", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        var rows = await connection.QueryAsync<AdverseEventNotificationItemDto>(new CommandDefinition($"SELECT notification_id AS NotificationId, ae_id AS AeId, trial_id AS TrialId, site_id AS SiteId, patient_id AS PatientId, ae_term_name AS AeTermName, ctcae_grade AS CtcaeGrade, serious AS Serious, priority AS Priority, outcome AS Outcome, acknowledged AS Acknowledged, sns_published AS SnsPublished, created_at AS CreatedAt FROM ae_notifications{whereClause} ORDER BY created_at DESC LIMIT @Limit OFFSET @Offset", parameters, cancellationToken: cancellationToken));
        return (total, rows.AsList());
    }
}