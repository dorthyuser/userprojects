using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Threading.Tasks;
using AdverseEventReporter.Helpers;
using AdverseEventReporter.Models;
using Npgsql;

namespace AdverseEventReporter.Services;

public class AdverseEventService
{
    private readonly DbHelper _db;

    public AdverseEventService(DbHelper db)
    {
        _db = db;
    }

    public async Task<AdverseEventResponse> SubmitAsync(AdverseEventCreateRequest? request)
    {
        if (request == null) throw new ApiException(HttpStatusCode.BadRequest, "MISSING_REQUIRED_FIELD", "Request body is required.");
        ValidateRequest(request);
        if (request.CtcaeGrade >= 3) request.Serious = true;
        if (request.CtcaeGrade == 5) request.Outcome = Outcome.FATAL;
        await using var connection = await _db.OpenConnectionAsync();
        await using var tx = await connection.BeginTransactionAsync();
        try
        {
            if (!await ExistsAsync(connection, tx, "SELECT 1 FROM trials WHERE trial_id = @trialId AND status = 'ACTIVE'", request.TrialId))
                throw new ApiException(HttpStatusCode.NotFound, "TRIAL_NOT_FOUND", "Trial not found or inactive.");
            if (!await ExistsAsync(connection, tx, "SELECT 1 FROM trial_enrolments WHERE trial_id = @trialId AND patient_id = @patientId AND status = 'ENROLLED'", request.TrialId, request.PatientId))
                throw new ApiException(HttpStatusCode.NotFound, "PATIENT_NOT_FOUND", "Patient not enrolled in trial.");
            if (await DuplicateExistsAsync(connection, tx, request))
                throw new ApiException(HttpStatusCode.Conflict, "DUPLICATE_AE", "Identical adverse event submitted within 60 seconds.");
            var aeId = await GenerateIdAsync(connection, tx, "ae_id_seq", "AE");
            var notifId = await GenerateIdAsync(connection, tx, "notif_id_seq", "NOTIF");
            var now = DateTimeOffset.UtcNow;
            await InsertAeAsync(connection, tx, aeId, request, now);
            await InsertNotificationAsync(connection, tx, notifId, aeId, request, now);
            await tx.CommitAsync();
            try { _ = false; } catch { }
            return new AdverseEventResponse { AeId = aeId, NotificationId = notifId, SnsPublished = false, SnsMessageId = null, ReceivedAt = now };
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<NotificationResponse> GetNotificationsAsync(NotificationQuery query)
    {
        if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100) throw new ApiException(HttpStatusCode.BadRequest, "INVALID_QUERY_PARAM", "Invalid paging parameters.");
        var conditions = new List<string>();
        var parameters = new List<NpgsqlParameter>();
        if (!string.IsNullOrWhiteSpace(query.TrialId)) { conditions.Add("trial_id = @trialId"); parameters.Add(new NpgsqlParameter("trialId", query.TrialId)); }
        if (!string.IsNullOrWhiteSpace(query.SiteId)) { conditions.Add("site_id = @siteId"); parameters.Add(new NpgsqlParameter("siteId", query.SiteId)); }
        if (query.CtcaeGrade.HasValue) { conditions.Add("ctcae_grade = @ctcaeGrade"); parameters.Add(new NpgsqlParameter("ctcaeGrade", query.CtcaeGrade.Value)); }
        if (query.Serious.HasValue) { conditions.Add("serious = @serious"); parameters.Add(new NpgsqlParameter("serious", query.Serious.Value)); }
        if (query.Acknowledged.HasValue) { conditions.Add("acknowledged = @acknowledged"); parameters.Add(new NpgsqlParameter("acknowledged", query.Acknowledged.Value)); }
        if (!string.IsNullOrWhiteSpace(query.Priority)) { if (query.Priority != "HIGH" && query.Priority != "NORMAL") throw new ApiException(HttpStatusCode.BadRequest, "INVALID_QUERY_PARAM", "Invalid priority."); conditions.Add("priority = @priority"); parameters.Add(new NpgsqlParameter("priority", query.Priority)); }
        if (query.DateFrom.HasValue) { conditions.Add("created_at >= @dateFrom"); parameters.Add(new NpgsqlParameter("dateFrom", query.DateFrom.Value)); }
        if (query.DateTo.HasValue) { conditions.Add("created_at <= @dateTo"); parameters.Add(new NpgsqlParameter("dateTo", query.DateTo.Value)); }
        var where = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : string.Empty;
        await using var connection = await _db.OpenConnectionAsync();
        var totalSql = "SELECT COUNT(*) FROM ae_notifications" + where;
        await using var totalCmd = new NpgsqlCommand(totalSql, connection);
        foreach (var p in parameters) totalCmd.Parameters.Add(new NpgsqlParameter(p.ParameterName, p.Value));
        var total = Convert.ToInt32(await totalCmd.ExecuteScalarAsync());
        var dataSql = "SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at FROM ae_notifications" + where + " ORDER BY created_at DESC LIMIT @limit OFFSET @offset";
        await using var dataCmd = new NpgsqlCommand(dataSql, connection);
        foreach (var p in parameters) dataCmd.Parameters.Add(new NpgsqlParameter(p.ParameterName, p.Value));
        dataCmd.Parameters.AddWithValue("limit", query.PageSize);
        dataCmd.Parameters.AddWithValue("offset", (query.Page - 1) * query.PageSize);
        var items = new List<NotificationItem>();
        await using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new NotificationItem
            {
                NotificationId = reader.GetString(0),
                AeId = reader.GetString(1),
                TrialId = reader.GetString(2),
                SiteId = reader.GetString(3),
                PatientId = reader.GetString(4),
                AeTermName = reader.GetString(5),
                CtcaeGrade = reader.GetInt16(6),
                Serious = reader.GetBoolean(7),
                Outcome = Enum.Parse<Outcome>(reader.GetString(8)),
                Priority = Enum.Parse<Priority>(reader.GetString(9)),
                Acknowledged = reader.GetBoolean(10),
                SnsPublished = reader.GetBoolean(11),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(12)
            });
        }
        return new NotificationResponse { Total = total, Page = query.Page, PageSize = query.PageSize, Notifications = items };
    }

    private static void ValidateRequest(AdverseEventCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TrialId) || string.IsNullOrWhiteSpace(request.SiteId) || string.IsNullOrWhiteSpace(request.PatientId) || string.IsNullOrWhiteSpace(request.ClinicianId) || string.IsNullOrWhiteSpace(request.AeTermCode) || string.IsNullOrWhiteSpace(request.AeTermName) || string.IsNullOrWhiteSpace(request.Narrative) || string.IsNullOrWhiteSpace(request.ReportedBy)) throw new ApiException(HttpStatusCode.BadRequest, "MISSING_REQUIRED_FIELD", "One or more required fields are missing.");
        if (request.CtcaeGrade < 1 || request.CtcaeGrade > 5) throw new ApiException(HttpStatusCode.BadRequest, "INVALID_CTCAE_GRADE", "ctcaeGrade must be between 1 and 5.");
        if (!Enum.IsDefined(typeof(Outcome), request.Outcome)) throw new ApiException(HttpStatusCode.BadRequest, "INVALID_OUTCOME", "Invalid outcome.");
        if (!Enum.IsDefined(typeof(ActionTaken), request.ActionTaken)) throw new ApiException(HttpStatusCode.BadRequest, "INVALID_ACTION_TAKEN", "Invalid actionTaken.");
        if (request.Narrative.Length > 2000) throw new ApiException(HttpStatusCode.BadRequest, "NARRATIVE_TOO_LONG", "Narrative exceeds 2000 characters.");
    }

    private static async Task<bool> ExistsAsync(NpgsqlConnection connection, NpgsqlTransaction tx, string sql, string trialId, string? patientId = null)
    {
        await using var cmd = new NpgsqlCommand(sql, connection, tx);
        cmd.Parameters.AddWithValue("trialId", trialId);
        if (patientId != null) cmd.Parameters.AddWithValue("patientId", patientId);
        var result = await cmd.ExecuteScalarAsync();
        return result != null;
    }

    private static async Task<bool> DuplicateExistsAsync(NpgsqlConnection connection, NpgsqlTransaction tx, AdverseEventCreateRequest request)
    {
        const string sql = "SELECT ae_id FROM adverse_events WHERE trial_id = @trialId AND patient_id = @patientId AND ae_term_code = @aeTermCode AND ctcae_grade = @ctcaeGrade AND submitted_at >= NOW() - INTERVAL '60 seconds' LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, connection, tx);
        cmd.Parameters.AddWithValue("trialId", request.TrialId);
        cmd.Parameters.AddWithValue("patientId", request.PatientId);
        cmd.Parameters.AddWithValue("aeTermCode", request.AeTermCode);
        cmd.Parameters.AddWithValue("ctcaeGrade", request.CtcaeGrade);
        var result = await cmd.ExecuteScalarAsync();
        return result != null;
    }

    private static async Task<string> GenerateIdAsync(NpgsqlConnection connection, NpgsqlTransaction tx, string sequenceName, string prefix)
    {
        await using var cmd = new NpgsqlCommand($"SELECT '{prefix}-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('{sequenceName}')::text, 6, '0')", connection, tx);
        return (string)(await cmd.ExecuteScalarAsync() ?? string.Empty);
    }

    private static async Task InsertAeAsync(NpgsqlConnection connection, NpgsqlTransaction tx, string aeId, AdverseEventCreateRequest request, DateTimeOffset now)
    {
        const string sql = @"INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (@aeId, @trialId, @siteId, @patientId, @clinicianId, @eventDate, @aeTermCode, @aeTermName, @ctcaeGrade, @serious, @outcome::outcome, @actionTaken::action_taken, @narrative, @relatedDrugId, @reportedBy, @submittedAt, @createdAt, @updatedAt)";
        await using var cmd = new NpgsqlCommand(sql, connection, tx);
        cmd.Parameters.AddWithValue("aeId", aeId);
        cmd.Parameters.AddWithValue("trialId", request.TrialId);
        cmd.Parameters.AddWithValue("siteId", request.SiteId);
        cmd.Parameters.AddWithValue("patientId", request.PatientId);
        cmd.Parameters.AddWithValue("clinicianId", request.ClinicianId);
        cmd.Parameters.AddWithValue("eventDate", request.EventDate.UtcDateTime);
        cmd.Parameters.AddWithValue("aeTermCode", request.AeTermCode);
        cmd.Parameters.AddWithValue("aeTermName", request.AeTermName);
        cmd.Parameters.AddWithValue("ctcaeGrade", request.CtcaeGrade);
        cmd.Parameters.AddWithValue("serious", request.Serious);
        cmd.Parameters.AddWithValue("outcome", request.Outcome.ToString());
        cmd.Parameters.AddWithValue("actionTaken", request.ActionTaken.ToString());
        cmd.Parameters.AddWithValue("narrative", request.Narrative);
        cmd.Parameters.AddWithValue("relatedDrugId", (object?)request.RelatedDrugId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("reportedBy", request.ReportedBy);
        cmd.Parameters.AddWithValue("submittedAt", now.UtcDateTime);
        cmd.Parameters.AddWithValue("createdAt", now.UtcDateTime);
        cmd.Parameters.AddWithValue("updatedAt", now.UtcDateTime);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task InsertNotificationAsync(NpgsqlConnection connection, NpgsqlTransaction tx, string notifId, string aeId, AdverseEventCreateRequest request, DateTimeOffset now)
    {
        const string sql = @"INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, acknowledged_by, acknowledged_at, sns_published, sns_message_id, created_at, updated_at) VALUES (@notificationId, @aeId, @trialId, @siteId, @patientId, @aeTermName, @ctcaeGrade, @serious, @outcome::outcome, @priority::priority, FALSE, NULL, NULL, FALSE, NULL, @createdAt, @updatedAt)";
        await using var cmd = new NpgsqlCommand(sql, connection, tx);
        cmd.Parameters.AddWithValue("notificationId", notifId);
        cmd.Parameters.AddWithValue("aeId", aeId);
        cmd.Parameters.AddWithValue("trialId", request.TrialId);
        cmd.Parameters.AddWithValue("siteId", request.SiteId);
        cmd.Parameters.AddWithValue("patientId", request.PatientId);
        cmd.Parameters.AddWithValue("aeTermName", request.AeTermName);
        cmd.Parameters.AddWithValue("ctcaeGrade", request.CtcaeGrade);
        cmd.Parameters.AddWithValue("serious", request.Serious);
        cmd.Parameters.AddWithValue("outcome", request.Outcome.ToString());
        cmd.Parameters.AddWithValue("priority", request.CtcaeGrade >= 3 ? Priority.HIGH.ToString() : Priority.NORMAL.ToString());
        cmd.Parameters.AddWithValue("createdAt", now.UtcDateTime);
        cmd.Parameters.AddWithValue("updatedAt", now.UtcDateTime);
        await cmd.ExecuteNonQueryAsync();
    }
}
