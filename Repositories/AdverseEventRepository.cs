using csharpapi248pm.Models;
using Npgsql;

namespace csharpapi248pm.Repositories;

public sealed class AdverseEventRepository : IAdverseEventRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<AdverseEventRepository> _logger;

    public AdverseEventRepository(NpgsqlDataSource dataSource, ILogger<AdverseEventRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task<AdverseEventSubmitResponse> SubmitAsync(AdverseEventRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DB operation start: INSERT adverse_events and ae_notifications");
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var aeId = await GenerateAeIdAsync(connection, cancellationToken);
            var notificationId = await GenerateNotificationIdAsync(connection, cancellationToken);
            var now = DateTime.UtcNow;
            await using (var cmd = connection.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandText = @"INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (@ae_id, @trial_id, @site_id, @patient_id, @clinician_id, @event_date, @ae_term_code, @ae_term_name, @ctcae_grade, @serious, @outcome, @action_taken, @narrative, @related_drug_id, @reported_by, @submitted_at, @created_at, @updated_at)";
                cmd.Parameters.AddWithValue("ae_id", aeId);
                cmd.Parameters.AddWithValue("trial_id", request.TrialId);
                cmd.Parameters.AddWithValue("site_id", request.SiteId);
                cmd.Parameters.AddWithValue("patient_id", request.PatientId);
                cmd.Parameters.AddWithValue("clinician_id", request.ClinicianId);
                cmd.Parameters.AddWithValue("event_date", request.EventDate);
                cmd.Parameters.AddWithValue("ae_term_code", request.AeTermCode);
                cmd.Parameters.AddWithValue("ae_term_name", request.AeTermName);
                cmd.Parameters.AddWithValue("ctcae_grade", request.CtcaeGrade);
                cmd.Parameters.AddWithValue("serious", request.Serious);
                cmd.Parameters.AddWithValue("outcome", request.Outcome.ToString());
                cmd.Parameters.AddWithValue("action_taken", request.ActionTaken.ToString());
                cmd.Parameters.AddWithValue("narrative", (object?)request.Narrative ?? DBNull.Value);
                cmd.Parameters.AddWithValue("related_drug_id", (object?)request.RelatedDrugId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("reported_by", request.ReportedBy);
                cmd.Parameters.AddWithValue("submitted_at", now);
                cmd.Parameters.AddWithValue("created_at", now);
                cmd.Parameters.AddWithValue("updated_at", now);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            await using (var cmd = connection.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandText = @"INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, sns_message_id, created_at, updated_at) VALUES (@notification_id, @ae_id, @trial_id, @site_id, @patient_id, @ae_term_name, @ctcae_grade, @serious, @outcome, @priority, FALSE, FALSE, NULL, @created_at, @updated_at)";
                cmd.Parameters.AddWithValue("notification_id", notificationId);
                cmd.Parameters.AddWithValue("ae_id", aeId);
                cmd.Parameters.AddWithValue("trial_id", request.TrialId);
                cmd.Parameters.AddWithValue("site_id", request.SiteId);
                cmd.Parameters.AddWithValue("patient_id", request.PatientId);
                cmd.Parameters.AddWithValue("ae_term_name", request.AeTermName);
                cmd.Parameters.AddWithValue("ctcae_grade", request.CtcaeGrade);
                cmd.Parameters.AddWithValue("serious", request.Serious);
                cmd.Parameters.AddWithValue("outcome", request.Outcome.ToString());
                cmd.Parameters.AddWithValue("priority", request.CtcaeGrade >= 3 ? PriorityEnum.HIGH.ToString() : PriorityEnum.NORMAL.ToString());
                cmd.Parameters.AddWithValue("created_at", now);
                cmd.Parameters.AddWithValue("updated_at", now);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("DB success: adverse_events {AeId}", aeId);
            _logger.LogInformation("DB success: ae_notifications {NotificationId}", notificationId);
            return new AdverseEventSubmitResponse { Status = "success", AeId = aeId, NotificationId = notificationId, SnsPublished = false, SnsMessageId = null, ReceivedAt = now };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB error INSERT adverse_events/ae_notifications");
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<AdverseEventNotificationsResponse> GetNotificationsAsync(AdverseEventNotificationsQueryRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DB operation start: SELECT ae_notifications");
        var conditions = new List<string>();
        var parameters = new List<NpgsqlParameter>();
        if (!string.IsNullOrWhiteSpace(request.TrialId)) { conditions.Add("trial_id = @trial_id"); parameters.Add(new NpgsqlParameter("trial_id", request.TrialId)); }
        if (!string.IsNullOrWhiteSpace(request.SiteId)) { conditions.Add("site_id = @site_id"); parameters.Add(new NpgsqlParameter("site_id", request.SiteId)); }
        if (request.CtcaeGrade.HasValue) { conditions.Add("ctcae_grade = @ctcae_grade"); parameters.Add(new NpgsqlParameter("ctcae_grade", request.CtcaeGrade.Value)); }
        if (request.Serious.HasValue) { conditions.Add("serious = @serious"); parameters.Add(new NpgsqlParameter("serious", request.Serious.Value)); }
        if (request.Acknowledged.HasValue) { conditions.Add("acknowledged = @acknowledged"); parameters.Add(new NpgsqlParameter("acknowledged", request.Acknowledged.Value)); }
        if (request.Priority.HasValue) { conditions.Add("priority = @priority"); parameters.Add(new NpgsqlParameter("priority", request.Priority.Value.ToString())); }
        if (request.DateFrom.HasValue) { conditions.Add("created_at >= @date_from"); parameters.Add(new NpgsqlParameter("date_from", request.DateFrom.Value)); }
        if (request.DateTo.HasValue) { conditions.Add("created_at <= @date_to"); parameters.Add(new NpgsqlParameter("date_to", request.DateTo.Value)); }
        var whereClause = conditions.Count == 0 ? "" : " WHERE " + string.Join(" AND ", conditions);
        var page = request.Page ?? 1;
        var pageSize = request.PageSize ?? 20;
        var total = 0;
        await using (var connection = await _dataSource.OpenConnectionAsync(cancellationToken))
        {
            try
            {
                await using (var countCmd = connection.CreateCommand())
                {
                    countCmd.CommandText = "SELECT COUNT(*) FROM ae_notifications" + whereClause;
                    countCmd.Parameters.AddRange(parameters.ToArray());
                    total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken));
                }
                var items = new List<AdverseEventNotificationItemResponse>();
                await using (var dataCmd = connection.CreateCommand())
                {
                    dataCmd.CommandText = "SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, sns_published, created_at FROM ae_notifications" + whereClause + " ORDER BY created_at DESC LIMIT @limit OFFSET @offset";
                    dataCmd.Parameters.AddRange(parameters.ToArray());
                    dataCmd.Parameters.AddWithValue("limit", pageSize);
                    dataCmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);
                    await using var reader = await dataCmd.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        items.Add(new AdverseEventNotificationItemResponse
                        {
                            NotificationId = reader.GetString(0),
                            AeId = reader.GetString(1),
                            TrialId = reader.GetString(2),
                            SiteId = reader.GetString(3),
                            PatientId = reader.GetString(4),
                            AeTermName = reader.GetString(5),
                            CtcaeGrade = reader.GetInt16(6),
                            Serious = reader.GetBoolean(7),
                            Priority = Enum.Parse<PriorityEnum>(reader.GetString(8)),
                            Outcome = Enum.Parse<OutcomeEnum>(reader.GetString(9)),
                            Acknowledged = reader.GetBoolean(10),
                            SnsPublished = reader.GetBoolean(11),
                            CreatedAt = reader.GetDateTime(12)
                        });
                    }
                }
                _logger.LogInformation("DB success: ae_notifications query");
                return new AdverseEventNotificationsResponse { Status = "success", Total = total, Page = page, PageSize = pageSize, Notifications = items };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DB error SELECT ae_notifications");
                throw;
            }
        }
    }

    private async Task<string> GenerateAeIdAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')";
        return (string)(await cmd.ExecuteScalarAsync(cancellationToken))!;
    }

    private async Task<string> GenerateNotificationIdAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')";
        return (string)(await cmd.ExecuteScalarAsync(cancellationToken))!;
    }
}