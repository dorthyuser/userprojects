using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using azurefunctionaeproject.Helpers;
using azurefunctionaeproject.Models;
using Npgsql;
using NpgsqlTypes;

namespace azurefunctionaeproject.Services;

public sealed class AdverseEventService
{
    private readonly DbHelper _dbHelper;

    public AdverseEventService(DbHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }

    public async Task<AdverseEventCreateResponse> SubmitAsync(AdverseEventCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TrialId) || string.IsNullOrWhiteSpace(request.SiteId) || string.IsNullOrWhiteSpace(request.PatientId) || string.IsNullOrWhiteSpace(request.ClinicianId) || string.IsNullOrWhiteSpace(request.AeTermCode) || string.IsNullOrWhiteSpace(request.AeTermName) || string.IsNullOrWhiteSpace(request.Narrative) || string.IsNullOrWhiteSpace(request.ReportedBy))
        {
            throw new ValidationException("MISSING_REQUIRED_FIELD", "One or more required fields are missing.");
        }

        if (request.CtcaeGrade < 1 || request.CtcaeGrade > 5)
        {
            throw new ValidationException("INVALID_CTCAE_GRADE", "ctcaeGrade must be between 1 and 5.");
        }

        if (request.Narrative.Length > 2000)
        {
            throw new ValidationException("NARRATIVE_TOO_LONG", "narrative exceeds 2000 characters.");
        }

        var outcome = request.Outcome;
        var serious = request.Serious || request.CtcaeGrade >= 3;
        if (request.CtcaeGrade == 5)
        {
            outcome = OutcomeEnum.FATAL;
        }

        await using var connection = await _dbHelper.OpenConnectionAsync();
        await using var tx = await connection.BeginTransactionAsync();

        var trialExists = await ExistsAsync(connection, tx, "SELECT 1 FROM trials WHERE trial_id = @trialId AND status = 'ACTIVE'", request.TrialId);
        if (!trialExists) throw new NotFoundException("TRIAL_NOT_FOUND", "Trial not found or inactive.");

        var patientExists = await ExistsPatientAsync(connection, tx, request.TrialId, request.PatientId);
        if (!patientExists) throw new NotFoundException("PATIENT_NOT_FOUND", "Patient not enrolled in the specified trial.");

        var duplicateAeId = await FindDuplicateAsync(connection, tx, request);
        if (duplicateAeId is not null) throw new ConflictException("DUPLICATE_AE", "Identical AE submitted within 60 seconds.", duplicateAeId);

        var aeId = await GenerateIdAsync(connection, tx, "ae_id_seq", "AE");
        var notificationId = await GenerateIdAsync(connection, tx, "notif_id_seq", "NOTIF");

        await InsertAdverseEventAsync(connection, tx, aeId, request, serious, outcome);
        await InsertNotificationAsync(connection, tx, aeId, notificationId, request, serious, outcome);
        await tx.CommitAsync();

        try
        {
            var snsPublished = false;
            var snsMessageId = (string?)null;
            _ = snsPublished;
            _ = snsMessageId;
        }
        catch
        {
        }

        return new AdverseEventCreateResponse
        {
            AeId = aeId,
            NotificationId = notificationId,
            SnsPublished = false,
            SnsMessageId = null,
            ReceivedAt = DateTime.UtcNow
        };
    }

    public async Task<NotificationResponse> GetNotificationsAsync(NotificationQuery query)
    {
        await using var connection = await _dbHelper.OpenConnectionAsync();
        var where = new List<string>();
        var parameters = new List<NpgsqlParameter>();

        if (!string.IsNullOrWhiteSpace(query.TrialId)) { where.Add("trial_id = @trialId"); parameters.Add(new NpgsqlParameter("trialId", query.TrialId)); }
        if (!string.IsNullOrWhiteSpace(query.SiteId)) { where.Add("site_id = @siteId"); parameters.Add(new NpgsqlParameter("siteId", query.SiteId)); }
        if (query.CtcaeGrade.HasValue) { where.Add("ctcae_grade = @ctcaeGrade"); parameters.Add(new NpgsqlParameter("ctcaeGrade", query.CtcaeGrade.Value)); }
        if (query.Serious.HasValue) { where.Add("serious = @serious"); parameters.Add(new NpgsqlParameter("serious", query.Serious.Value)); }
        if (query.Acknowledged.HasValue) { where.Add("acknowledged = @acknowledged"); parameters.Add(new NpgsqlParameter("acknowledged", query.Acknowledged.Value)); }
        if (query.Priority.HasValue) { where.Add("priority = @priority::priority_enum"); parameters.Add(new NpgsqlParameter("priority", query.Priority.Value.ToString())); }
        if (query.DateFrom.HasValue) { where.Add("created_at >= @dateFrom"); parameters.Add(new NpgsqlParameter("dateFrom", query.DateFrom.Value)); }
        if (query.DateTo.HasValue) { where.Add("created_at <= @dateTo"); parameters.Add(new NpgsqlParameter("dateTo", query.DateTo.Value)); }

        var whereClause = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : string.Empty;
        var countSql = "SELECT COUNT(*) FROM ae_notifications" + whereClause;
        await using var countCmd = new NpgsqlCommand(countSql, connection);
        foreach (var p in parameters) countCmd.Parameters.Add(new NpgsqlParameter(p.ParameterName, p.Value));
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = "SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, sns_published, created_at FROM ae_notifications" + whereClause + " ORDER BY created_at DESC LIMIT @limit OFFSET @offset";
        await using var dataCmd = new NpgsqlCommand(dataSql, connection);
        foreach (var p in parameters) dataCmd.Parameters.Add(new NpgsqlParameter(p.ParameterName, p.Value));
        dataCmd.Parameters.AddWithValue("limit", query.PageSize);
        dataCmd.Parameters.AddWithValue("offset", (query.Page - 1) * query.PageSize);

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
                Priority = Enum.Parse<PriorityEnum>(reader.GetString(8), true),
                Outcome = Enum.Parse<OutcomeEnum>(reader.GetString(9), true),
                Acknowledged = reader.GetBoolean(10),
                SnsPublished = reader.GetBoolean(11),
                CreatedAt = reader.GetDateTime(12)
            });
        }

        return new NotificationResponse { Total = total, Page = query.Page, PageSize = query.PageSize, Notifications = notifications };
    }

    private static async Task<bool> ExistsAsync(NpgsqlConnection connection, NpgsqlTransaction tx, string sql, string trialId)
    {
        await using var cmd = new NpgsqlCommand(sql, connection, tx);
        cmd.Parameters.AddWithValue("trialId", trialId);
        var result = await cmd.ExecuteScalarAsync();
        return result is not null;
    }

    private static async Task<bool> ExistsPatientAsync(NpgsqlConnection connection, NpgsqlTransaction tx, string trialId, string patientId)
    {
        await using var cmd = new NpgsqlCommand("SELECT 1 FROM trial_enrolments WHERE trial_id = @trialId AND patient_id = @patientId AND status = 'ENROLLED'", connection, tx);
        cmd.Parameters.AddWithValue("trialId", trialId);
        cmd.Parameters.AddWithValue("patientId", patientId);
        var result = await cmd.ExecuteScalarAsync();
        return result is not null;
    }

    private static async Task<string?> FindDuplicateAsync(NpgsqlConnection connection, NpgsqlTransaction tx, AdverseEventCreateRequest request)
    {
        await using var cmd = new NpgsqlCommand("SELECT ae_id FROM adverse_events WHERE trial_id = @trialId AND patient_id = @patientId AND ae_term_code = @aeTermCode AND ctcae_grade = @ctcaeGrade AND submitted_at >= NOW() - INTERVAL '60 seconds' LIMIT 1", connection, tx);
        cmd.Parameters.AddWithValue("trialId", request.TrialId);
        cmd.Parameters.AddWithValue("patientId", request.PatientId);
        cmd.Parameters.AddWithValue("aeTermCode", request.AeTermCode);
        cmd.Parameters.AddWithValue("ctcaeGrade", request.CtcaeGrade);
        var result = await cmd.ExecuteScalarAsync();
        return result?.ToString();
    }

    private static async Task<string> GenerateIdAsync(NpgsqlConnection connection, NpgsqlTransaction tx, string sequenceName, string prefix)
    {
        await using var cmd = new NpgsqlCommand($"SELECT '{prefix}-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('{sequenceName}')::text, 6, '0')", connection, tx);
        return (await cmd.ExecuteScalarAsync())?.ToString() ?? string.Empty;
    }

    private static async Task InsertAdverseEventAsync(NpgsqlConnection connection, NpgsqlTransaction tx, string aeId, AdverseEventCreateRequest request, bool serious, OutcomeEnum outcome)
    {
        await using var cmd = new NpgsqlCommand(@"INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (@aeId, @trialId, @siteId, @patientId, @clinicianId, @eventDate, @aeTermCode, @aeTermName, @ctcaeGrade, @serious, @outcome::outcome_enum, @actionTaken::action_taken_enum, @narrative, @relatedDrugId, @reportedBy, NOW(), NOW(), NOW())", connection, tx);
        cmd.Parameters.AddWithValue("aeId", aeId);
        cmd.Parameters.AddWithValue("trialId", request.TrialId);
        cmd.Parameters.AddWithValue("siteId", request.SiteId);
        cmd.Parameters.AddWithValue("patientId", request.PatientId);
        cmd.Parameters.AddWithValue("clinicianId", request.ClinicianId);
        cmd.Parameters.AddWithValue("eventDate", request.EventDate.ToUniversalTime());
        cmd.Parameters.AddWithValue("aeTermCode", request.AeTermCode);
        cmd.Parameters.AddWithValue("aeTermName", request.AeTermName);
        cmd.Parameters.AddWithValue("ctcaeGrade", request.CtcaeGrade);
        cmd.Parameters.AddWithValue("serious", serious);
        cmd.Parameters.AddWithValue("outcome", outcome.ToString());
        cmd.Parameters.AddWithValue("actionTaken", request.ActionTaken.ToString());
        cmd.Parameters.AddWithValue("narrative", request.Narrative);
        cmd.Parameters.AddWithValue("relatedDrugId", (object?)request.RelatedDrugId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("reportedBy", request.ReportedBy);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task InsertNotificationAsync(NpgsqlConnection connection, NpgsqlTransaction tx, string aeId, string notificationId, AdverseEventCreateRequest request, bool serious, OutcomeEnum outcome)
    {
        await using var cmd = new NpgsqlCommand(@"INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, sns_message_id, created_at, updated_at) VALUES (@notificationId, @aeId, @trialId, @siteId, @patientId, @aeTermName, @ctcaeGrade, @serious, @outcome::outcome_enum, @priority::priority_enum, FALSE, FALSE, NULL, NOW(), NOW())", connection, tx);
        cmd.Parameters.AddWithValue("notificationId", notificationId);
        cmd.Parameters.AddWithValue("aeId", aeId);
        cmd.Parameters.AddWithValue("trialId", request.TrialId);
        cmd.Parameters.AddWithValue("siteId", request.SiteId);
        cmd.Parameters.AddWithValue("patientId", request.PatientId);
        cmd.Parameters.AddWithValue("aeTermName", request.AeTermName);
        cmd.Parameters.AddWithValue("ctcaeGrade", request.CtcaeGrade);
        cmd.Parameters.AddWithValue("serious", serious);
        cmd.Parameters.AddWithValue("outcome", outcome.ToString());
        cmd.Parameters.AddWithValue("priority", request.CtcaeGrade >= 3 ? PriorityEnum.HIGH.ToString() : PriorityEnum.NORMAL.ToString());
        await cmd.ExecuteNonQueryAsync();
    }
}
