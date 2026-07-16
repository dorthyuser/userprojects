using System.Data;
using System.Text.RegularExpressions;
using Amazon.Lambda.Core;
using Npgsql;
using Aelambda1024Lambda.Models;

namespace Aelambda1024Lambda.Services;

public sealed class Service
{
    private readonly NpgsqlDataSource _dataSource;

    public Service(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<AdverseEventResponse> SubmitAdverseEventAsync(AdverseEventRequest? request, ILambdaContext context)
    {
        ValidatePostRequest(request);

        var normalized = Normalize(request!);
        await EnsureTrialExistsAsync(normalized.TrialId!);
        await EnsurePatientEnrolledAsync(normalized.TrialId!, normalized.PatientId!);
        var existingAeId = await FindDuplicateAsync(normalized);
        if (!string.IsNullOrWhiteSpace(existingAeId)) throw new ApiException(409, "DUPLICATE_AE", "Identical AE submitted within 60 seconds", existingAeId);

        var aeId = await GenerateAeIdAsync();
        var notificationId = await GenerateNotificationIdAsync();
        var receivedAt = DateTimeOffset.UtcNow;

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            const string insertAeSql = @"INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (@ae_id, @trial_id, @site_id, @patient_id, @clinician_id, @event_date, @ae_term_code, @ae_term_name, @ctcae_grade, @serious, @outcome, @action_taken, @narrative, @related_drug_id, @reported_by, @submitted_at, NOW(), NOW())";
            await using (var cmd = new NpgsqlCommand(insertAeSql, conn, tx))
            {
                cmd.Parameters.AddWithValue("ae_id", aeId);
                cmd.Parameters.AddWithValue("trial_id", normalized.TrialId!);
                cmd.Parameters.AddWithValue("site_id", normalized.SiteId!);
                cmd.Parameters.AddWithValue("patient_id", normalized.PatientId!);
                cmd.Parameters.AddWithValue("clinician_id", normalized.ClinicianId!);
                cmd.Parameters.AddWithValue("event_date", normalized.EventDate!.Value);
                cmd.Parameters.AddWithValue("ae_term_code", normalized.AeTermCode!);
                cmd.Parameters.AddWithValue("ae_term_name", normalized.AeTermName!);
                cmd.Parameters.AddWithValue("ctcae_grade", normalized.CtcaeGrade!.Value);
                cmd.Parameters.AddWithValue("serious", normalized.Serious!.Value);
                cmd.Parameters.AddWithValue("outcome", normalized.Outcome!.Value);
                cmd.Parameters.AddWithValue("action_taken", normalized.ActionTaken!.Value);
                cmd.Parameters.AddWithValue("narrative", (object?)normalized.Narrative ?? DBNull.Value);
                cmd.Parameters.AddWithValue("related_drug_id", (object?)normalized.RelatedDrugId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("reported_by", normalized.ReportedBy!);
                cmd.Parameters.AddWithValue("submitted_at", receivedAt);
                await cmd.ExecuteNonQueryAsync();
            }

            const string insertNotifSql = @"INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, sns_message_id, created_at, updated_at) VALUES (@notification_id, @ae_id, @trial_id, @site_id, @patient_id, @ae_term_name, @ctcae_grade, @serious, @outcome, @priority, FALSE, FALSE, NULL, NOW(), NOW())";
            await using (var cmd = new NpgsqlCommand(insertNotifSql, conn, tx))
            {
                cmd.Parameters.AddWithValue("notification_id", notificationId);
                cmd.Parameters.AddWithValue("ae_id", aeId);
                cmd.Parameters.AddWithValue("trial_id", normalized.TrialId!);
                cmd.Parameters.AddWithValue("site_id", normalized.SiteId!);
                cmd.Parameters.AddWithValue("patient_id", normalized.PatientId!);
                cmd.Parameters.AddWithValue("ae_term_name", normalized.AeTermName!);
                cmd.Parameters.AddWithValue("ctcae_grade", normalized.CtcaeGrade!.Value);
                cmd.Parameters.AddWithValue("serious", normalized.Serious!.Value);
                cmd.Parameters.AddWithValue("outcome", normalized.Outcome!.Value);
                cmd.Parameters.AddWithValue("priority", normalized.CtcaeGrade >= 3 ? NotificationPriority.HIGH : NotificationPriority.NORMAL);
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw new ApiException(500, "DB_ERROR", "PostgreSQL write failure");
        }

        try { _ = false; } catch { }

        return new AdverseEventResponse
        {
            AeId = aeId,
            NotificationId = notificationId,
            SnsPublished = false,
            SnsMessageId = null,
            ReceivedAt = receivedAt
        };
    }

    public async Task<NotificationsResponse> GetNotificationsAsync(Dictionary<string, string>? query, ILambdaContext context)
    {
        var parsed = ParseQuery(query);
        var conditions = new List<string>();
        var parameters = new List<NpgsqlParameter>();

        if (!string.IsNullOrWhiteSpace(parsed.TrialId)) { conditions.Add("trial_id = @trial_id"); parameters.Add(new NpgsqlParameter("trial_id", parsed.TrialId)); }
        if (!string.IsNullOrWhiteSpace(parsed.SiteId)) { conditions.Add("site_id = @site_id"); parameters.Add(new NpgsqlParameter("site_id", parsed.SiteId)); }
        if (parsed.CtcaeGrade.HasValue) { conditions.Add("ctcae_grade = @ctcae_grade"); parameters.Add(new NpgsqlParameter("ctcae_grade", parsed.CtcaeGrade.Value)); }
        if (parsed.Serious.HasValue) { conditions.Add("serious = @serious"); parameters.Add(new NpgsqlParameter("serious", parsed.Serious.Value)); }
        if (parsed.Acknowledged.HasValue) { conditions.Add("acknowledged = @acknowledged"); parameters.Add(new NpgsqlParameter("acknowledged", parsed.Acknowledged.Value)); }
        if (parsed.Priority.HasValue) { conditions.Add("priority = @priority"); parameters.Add(new NpgsqlParameter("priority", parsed.Priority.Value)); }
        if (parsed.DateFrom.HasValue) { conditions.Add("created_at >= @date_from"); parameters.Add(new NpgsqlParameter("date_from", parsed.DateFrom.Value)); }
        if (parsed.DateTo.HasValue) { conditions.Add("created_at <= @date_to"); parameters.Add(new NpgsqlParameter("date_to", parsed.DateTo.Value)); }

        var whereClause = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : string.Empty;
        var offset = (parsed.Page - 1) * parsed.PageSize;

        await using var conn = await _dataSource.OpenConnectionAsync();
        var totalSql = "SELECT COUNT(*) FROM ae_notifications" + whereClause;
        await using var totalCmd = new NpgsqlCommand(totalSql, conn);
        foreach (var p in parameters) totalCmd.Parameters.Add(new NpgsqlParameter(p.ParameterName, p.Value));
        var total = Convert.ToInt32(await totalCmd.ExecuteScalarAsync());

        var dataSql = "SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at FROM ae_notifications" + whereClause + " ORDER BY created_at DESC LIMIT @limit OFFSET @offset";
        await using var dataCmd = new NpgsqlCommand(dataSql, conn);
        foreach (var p in parameters) dataCmd.Parameters.Add(new NpgsqlParameter(p.ParameterName, p.Value));
        dataCmd.Parameters.AddWithValue("limit", parsed.PageSize);
        dataCmd.Parameters.AddWithValue("offset", offset);

        var notifications = new List<NotificationItem>();
        await using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            notifications.Add(new NotificationItem
            {
                NotificationId = reader.GetString(0),
                AeId = reader.GetString(1),
                TrialId = reader.GetString(2),
                SiteId = reader.GetString(3),
                PatientId = reader.GetString(4),
                AeTermName = reader.GetString(5),
                CtcaeGrade = reader.GetInt16(6),
                Serious = reader.GetBoolean(7),
                Outcome = reader.GetFieldValue<AeOutcome>(8),
                Priority = reader.GetFieldValue<NotificationPriority>(9),
                Acknowledged = reader.GetBoolean(10),
                SnsPublished = reader.GetBoolean(11),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(12)
            });
        }

        return new NotificationsResponse { Total = total, Page = parsed.Page, PageSize = parsed.PageSize, Notifications = notifications };
    }

    private static void ValidatePostRequest(AdverseEventRequest? request)
    {
        if (request is null) throw new ApiException(400, "MISSING_REQUIRED_FIELD", "Request body is required");
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(request.TrialId)) missing.Add("trialId");
        if (string.IsNullOrWhiteSpace(request.SiteId)) missing.Add("siteId");
        if (string.IsNullOrWhiteSpace(request.PatientId)) missing.Add("patientId");
        if (string.IsNullOrWhiteSpace(request.ClinicianId)) missing.Add("clinicianId");
        if (request.EventDate is null) missing.Add("eventDate");
        if (string.IsNullOrWhiteSpace(request.AeTermCode)) missing.Add("aeTermCode");
        if (string.IsNullOrWhiteSpace(request.AeTermName)) missing.Add("aeTermName");
        if (request.CtcaeGrade is null) missing.Add("ctcaeGrade");
        if (request.Serious is null) missing.Add("serious");
        if (request.Outcome is null) missing.Add("outcome");
        if (request.ActionTaken is null) missing.Add("actionTaken");
        if (string.IsNullOrWhiteSpace(request.Narrative)) missing.Add("narrative");
        if (string.IsNullOrWhiteSpace(request.ReportedBy)) missing.Add("reportedBy");
        if (missing.Count > 0) throw new ApiException(400, "MISSING_REQUIRED_FIELD", $"Missing required fields: {string.Join(", ", missing)}");
        if (request.CtcaeGrade < 1 || request.CtcaeGrade > 5) throw new ApiException(400, "INVALID_CTCAE_GRADE", "ctcaeGrade not an integer between 1 and 5");
        if (request.Narrative!.Length > 2000) throw new ApiException(400, "NARRATIVE_TOO_LONG", "narrative exceeds 2000 characters");
        if (!Enum.IsDefined(typeof(AeOutcome), request.Outcome)) throw new ApiException(400, "INVALID_OUTCOME", "Invalid value for field 'outcome'. Accepted values: ONGOING, RESOLVED, FATAL, UNKNOWN");
        if (!Enum.IsDefined(typeof(AeActionTaken), request.ActionTaken)) throw new ApiException(400, "INVALID_ACTION_TAKEN", "Invalid value for field 'actionTaken'. Accepted values: NONE, DOSE_REDUCED, DRUG_WITHDRAWN, HOSPITALISED");
    }

    private static AdverseEventRequest Normalize(AdverseEventRequest request)
    {
        request.Serious = request.CtcaeGrade >= 3;
        if (request.CtcaeGrade == 5) request.Outcome = AeOutcome.FATAL;
        return request;
    }

    private async Task EnsureTrialExistsAsync(string trialId)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand("SELECT id FROM trials WHERE trial_id = @trial_id AND status = 'ACTIVE'", conn);
        cmd.Parameters.AddWithValue("trial_id", trialId);
        var result = await cmd.ExecuteScalarAsync();
        if (result is null) throw new ApiException(404, "TRIAL_NOT_FOUND", "trialId does not exist or is not active");
    }

    private async Task EnsurePatientEnrolledAsync(string trialId, string patientId)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand("SELECT id FROM trial_enrolments WHERE trial_id = @trial_id AND patient_id = @patient_id AND status = 'ENROLLED'", conn);
        cmd.Parameters.AddWithValue("trial_id", trialId);
        cmd.Parameters.AddWithValue("patient_id", patientId);
        var result = await cmd.ExecuteScalarAsync();
        if (result is null) throw new ApiException(404, "PATIENT_NOT_FOUND", "patientId not enrolled in the specified trial");
    }

    private async Task<string?> FindDuplicateAsync(AdverseEventRequest request)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        const string sql = @"SELECT ae_id FROM adverse_events WHERE trial_id = @trial_id AND patient_id = @patient_id AND ae_term_code = @ae_term_code AND ctcae_grade = @ctcae_grade AND submitted_at >= NOW() - INTERVAL '60 seconds' LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("trial_id", request.TrialId!);
        cmd.Parameters.AddWithValue("patient_id", request.PatientId!);
        cmd.Parameters.AddWithValue("ae_term_code", request.AeTermCode!);
        cmd.Parameters.AddWithValue("ctcae_grade", request.CtcaeGrade!.Value);
        var result = await cmd.ExecuteScalarAsync();
        return result?.ToString();
    }

    private async Task<string> GenerateAeIdAsync()
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')", conn);
        return (string)(await cmd.ExecuteScalarAsync())!;
    }

    private async Task<string> GenerateNotificationIdAsync()
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')", conn);
        return (string)(await cmd.ExecuteScalarAsync())!;
    }

    private static NotificationQuery ParseQuery(Dictionary<string, string>? query)
    {
        var q = new NotificationQuery();
        if (query is null) return q;
        if (query.TryGetValue("trialId", out var trialId)) q.TrialId = trialId;
        if (query.TryGetValue("siteId", out var siteId)) q.SiteId = siteId;
        if (query.TryGetValue("ctcaeGrade", out var grade) && int.TryParse(grade, out var g) && g >= 1 && g <= 5) q.CtcaeGrade = g; else if (query.ContainsKey("ctcaeGrade")) throw new ApiException(400, "INVALID_QUERY_PARAM", "Invalid query parameter: ctcaeGrade");
        if (query.TryGetValue("serious", out var serious) && bool.TryParse(serious, out var s)) q.Serious = s; else if (query.ContainsKey("serious")) throw new ApiException(400, "INVALID_QUERY_PARAM", "Invalid query parameter: serious");
        if (query.TryGetValue("acknowledged", out var ack) && bool.TryParse(ack, out var a)) q.Acknowledged = a; else if (query.ContainsKey("acknowledged")) throw new ApiException(400, "INVALID_QUERY_PARAM", "Invalid query parameter: acknowledged");
        if (query.TryGetValue("priority", out var priority) && Enum.TryParse<NotificationPriority>(priority, true, out var p) && Enum.IsDefined(typeof(NotificationPriority), p)) q.Priority = p; else if (query.ContainsKey("priority")) throw new ApiException(400, "INVALID_QUERY_PARAM", "Invalid query parameter: priority");
        if (query.TryGetValue("dateFrom", out var df) && DateTimeOffset.TryParse(df, out var dfo)) q.DateFrom = dfo; else if (query.ContainsKey("dateFrom")) throw new ApiException(400, "INVALID_QUERY_PARAM", "Invalid query parameter: dateFrom");
        if (query.TryGetValue("dateTo", out var dt) && DateTimeOffset.TryParse(dt, out var dto)) q.DateTo = dto; else if (query.ContainsKey("dateTo")) throw new ApiException(400, "INVALID_QUERY_PARAM", "Invalid query parameter: dateTo");
        if (query.TryGetValue("page", out var page) && int.TryParse(page, out var pg) && pg >= 1) q.Page = pg; else if (query.ContainsKey("page")) throw new ApiException(400, "INVALID_QUERY_PARAM", "Invalid query parameter: page");
        if (query.TryGetValue("pageSize", out var pageSize) && int.TryParse(pageSize, out var ps) && ps >= 1 && ps <= 100) q.PageSize = ps; else if (query.ContainsKey("pageSize")) throw new ApiException(400, "INVALID_QUERY_PARAM", "Invalid query parameter: pageSize");
        return q;
    }
}

public sealed class ApiException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }
    public string? ExistingAeId { get; }

    public ApiException(int statusCode, string code, string message, string? existingAeId = null) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        ExistingAeId = existingAeId;
    }
}