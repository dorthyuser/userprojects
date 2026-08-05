using Microsoft.Extensions.Logging;
using Npgsql;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using Csharpae1140Lambda.Models;
using MSLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Csharpae1140Lambda.Services;

public sealed class Service
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<Service> _logger;

    public Service(NpgsqlDataSource dataSource, ILogger<Service> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task<SubmitAdverseEventResponse> SubmitAdverseEventAsync(string? body, string requestId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating request...");
        if (string.IsNullOrWhiteSpace(body)) throw new ValidationException("Request body is missing.");
        var request = JsonSerializer.Deserialize<AdverseEventRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new ValidationException("Malformed request body.");
        ValidateRequest(request);
        ApplyCoercions(request);
        _logger.LogInformation("Validation passed.");
        var receivedAt = DateTime.UtcNow;
        var aeId = await GenerateAeIdAsync(cancellationToken);
        var notificationId = await GenerateNotificationIdAsync(cancellationToken);
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            await InsertAeAsync(conn, tx, request, aeId, receivedAt, cancellationToken);
            await InsertNotificationAsync(conn, tx, request, aeId, notificationId, cancellationToken);
            await InsertAuditAsync(conn, tx, request, aeId, cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return new SubmitAdverseEventResponse { Status = "success", AeId = aeId, NotificationId = notificationId, Message = "Adverse event recorded and notification stored.", ReceivedAt = receivedAt };
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<NotificationListResponse> GetNotificationsAsync(IDictionary<string, string>? queryParams, string requestId, CancellationToken cancellationToken)
    {
        var page = 1;
        var pageSize = 20;
        if (queryParams is not null)
        {
            if (queryParams.TryGetValue("page", out var pageValue) && int.TryParse(pageValue, out var parsedPage) && parsedPage > 0) page = parsedPage;
            if (queryParams.TryGetValue("pageSize", out var pageSizeValue) && int.TryParse(pageSizeValue, out var parsedPageSize) && parsedPageSize > 0) pageSize = parsedPageSize;
        }
        if (pageSize > 100) throw new ValidationException("pageSize exceeds maximum of 100.");
        var where = new List<string>();
        var parameters = new List<NpgsqlParameter>();
        AddFilter(queryParams, "trialId", "trial_id", where, parameters);
        AddFilter(queryParams, "siteId", "site_id", where, parameters);
        AddIntFilter(queryParams, "ctcaeGrade", "ctcae_grade", where, parameters);
        AddBoolFilter(queryParams, "serious", "serious", where, parameters);
        AddBoolFilter(queryParams, "acknowledged", "acknowledged", where, parameters);
        AddFilter(queryParams, "priority", "priority", where, parameters);
        AddDateFilter(queryParams, "dateFrom", "created_at >=", where, parameters);
        AddDateFilter(queryParams, "dateTo", "created_at <=", where, parameters);
        var whereSql = where.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", where);
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        var totalSql = "SELECT COUNT(*) FROM ae_notifications" + whereSql;
        await using var totalCmd = new NpgsqlCommand(totalSql, conn);
        foreach (var p in parameters) totalCmd.Parameters.Add(p.Clone());
        var total = (long)(await totalCmd.ExecuteScalarAsync(cancellationToken) ?? 0L);
        var selectSql = "SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, acknowledged_by, acknowledged_at, created_at FROM ae_notifications" + whereSql + " ORDER BY created_at DESC LIMIT @limit OFFSET @offset";
        await using var cmd = new NpgsqlCommand(selectSql, conn);
        foreach (var p in parameters) cmd.Parameters.Add(p.Clone());
        cmd.Parameters.AddWithValue("limit", pageSize);
        cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);
        var items = new List<NotificationItemResponse>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new NotificationItemResponse
            {
                NotificationId = reader.GetString(0), AeId = reader.GetString(1), TrialId = reader.GetString(2), SiteId = reader.GetString(3), PatientId = reader.GetString(4), AeTermName = reader.GetString(5), CtcaeGrade = reader.GetInt16(6), Serious = reader.GetBoolean(7), Priority = reader.GetString(8), Outcome = reader.GetString(9), Acknowledged = reader.GetBoolean(10), AcknowledgedBy = reader.IsDBNull(11) ? null : reader.GetString(11), AcknowledgedAt = reader.IsDBNull(12) ? null : reader.GetDateTime(12), CreatedAt = reader.GetDateTime(13)
            });
        }
        return new NotificationListResponse { Status = "success", Total = total, Page = page, PageSize = pageSize, Notifications = items };
    }

    private static void ValidateRequest(AdverseEventRequest request)
    {
        var ctx = new ValidationContext(request);
        Validator.ValidateObject(request, ctx, true);
        if (request.CtcaeGrade is null || request.CtcaeGrade < 1 || request.CtcaeGrade > 5) throw new ValidationException("INVALID_CTCAE_GRADE");
        if (request.Narrative is not null && request.Narrative.Length > 2000) throw new ValidationException("NARRATIVE_TOO_LONG");
    }

    private static void ApplyCoercions(AdverseEventRequest request)
    {
        if (request.CtcaeGrade >= 3) request.Serious = true;
        if (request.CtcaeGrade == 5) request.Outcome = OutcomeEnum.FATAL;
    }

    private async Task<string> GenerateAeIdAsync(CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT nextval('ae_id_seq')", conn);
        var seq = Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        return $"AE-{DateTime.UtcNow:yyyy}-{seq:D6}";
    }

    private async Task<string> GenerateNotificationIdAsync(CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT nextval('notif_id_seq')", conn);
        var seq = Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        return $"NOTIF-{DateTime.UtcNow:yyyy}-{seq:D6}";
    }

    private static async Task InsertAeAsync(NpgsqlConnection conn, NpgsqlTransaction tx, AdverseEventRequest request, string aeId, DateTime receivedAt, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand("INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at) VALUES (@ae_id, @trial_id, @site_id, @patient_id, @clinician_id, @event_date, @ae_term_code, @ae_term_name, @ctcae_grade, @serious, @outcome, @action_taken, @narrative, @related_drug_id, @reported_by, @submitted_at)", conn, tx);
        cmd.Parameters.AddWithValue("ae_id", aeId);
        cmd.Parameters.AddWithValue("trial_id", request.TrialId!);
        cmd.Parameters.AddWithValue("site_id", request.SiteId!);
        cmd.Parameters.AddWithValue("patient_id", request.PatientId!);
        cmd.Parameters.AddWithValue("clinician_id", request.ClinicianId!);
        cmd.Parameters.AddWithValue("event_date", DateTime.SpecifyKind(request.EventDate!.Value, DateTimeKind.Utc));
        cmd.Parameters.AddWithValue("ae_term_code", request.AeTermCode!);
        cmd.Parameters.AddWithValue("ae_term_name", request.AeTermName!);
        cmd.Parameters.AddWithValue("ctcae_grade", request.CtcaeGrade!.Value);
        cmd.Parameters.AddWithValue("serious", request.Serious!.Value);
        cmd.Parameters.AddWithValue("outcome", request.Outcome!.Value);
        cmd.Parameters.AddWithValue("action_taken", request.ActionTaken!.Value);
        cmd.Parameters.AddWithValue("narrative", (object?)request.Narrative ?? DBNull.Value);
        cmd.Parameters.AddWithValue("related_drug_id", (object?)request.RelatedDrugId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("reported_by", request.ReportedBy!);
        cmd.Parameters.AddWithValue("submitted_at", receivedAt);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertNotificationAsync(NpgsqlConnection conn, NpgsqlTransaction tx, AdverseEventRequest request, string aeId, string notificationId, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand("INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged) VALUES (@notification_id, @ae_id, @trial_id, @site_id, @patient_id, @ae_term_name, @ctcae_grade, @serious, @outcome, @priority, FALSE)", conn, tx);
        cmd.Parameters.AddWithValue("notification_id", notificationId);
        cmd.Parameters.AddWithValue("ae_id", aeId);
        cmd.Parameters.AddWithValue("trial_id", request.TrialId!);
        cmd.Parameters.AddWithValue("site_id", request.SiteId!);
        cmd.Parameters.AddWithValue("patient_id", request.PatientId!);
        cmd.Parameters.AddWithValue("ae_term_name", request.AeTermName!);
        cmd.Parameters.AddWithValue("ctcae_grade", request.CtcaeGrade!.Value);
        cmd.Parameters.AddWithValue("serious", request.Serious!.Value);
        cmd.Parameters.AddWithValue("outcome", request.Outcome!.Value);
        cmd.Parameters.AddWithValue("priority", request.CtcaeGrade >= 3 ? PriorityEnum.HIGH : PriorityEnum.NORMAL);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertAuditAsync(NpgsqlConnection conn, NpgsqlTransaction tx, AdverseEventRequest request, string aeId, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand("INSERT INTO ae_audit_log (ae_id, action, performed_by, sae, notes) VALUES (@ae_id, @action, @performed_by, @sae, @notes)", conn, tx);
        cmd.Parameters.AddWithValue("ae_id", aeId);
        cmd.Parameters.AddWithValue("action", "CREATED");
        cmd.Parameters.AddWithValue("performed_by", request.ReportedBy!);
        cmd.Parameters.AddWithValue("sae", request.Serious!.Value);
        cmd.Parameters.AddWithValue("notes", (object?)request.Narrative ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddFilter(IDictionary<string, string>? queryParams, string queryKey, string column, List<string> where, List<NpgsqlParameter> parameters)
    {
        if (queryParams is null || !queryParams.TryGetValue(queryKey, out var value) || string.IsNullOrWhiteSpace(value)) return;
        where.Add($"{column} = @{queryKey}");
        parameters.Add(new NpgsqlParameter(queryKey, value));
    }

    private static void AddIntFilter(IDictionary<string, string>? queryParams, string queryKey, string column, List<string> where, List<NpgsqlParameter> parameters)
    {
        if (queryParams is null || !queryParams.TryGetValue(queryKey, out var value) || !int.TryParse(value, out var parsed)) return;
        where.Add($"{column} = @{queryKey}");
        parameters.Add(new NpgsqlParameter(queryKey, parsed));
    }

    private static void AddBoolFilter(IDictionary<string, string>? queryParams, string queryKey, string column, List<string> where, List<NpgsqlParameter> parameters)
    {
        if (queryParams is null || !queryParams.TryGetValue(queryKey, out var value) || !bool.TryParse(value, out var parsed)) return;
        where.Add($"{column} = @{queryKey}");
        parameters.Add(new NpgsqlParameter(queryKey, parsed));
    }

    private static void AddDateFilter(IDictionary<string, string>? queryParams, string queryKey, string column, List<string> where, List<NpgsqlParameter> parameters)
    {
        if (queryParams is null || !queryParams.TryGetValue(queryKey, out var value) || !DateTime.TryParse(value, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed)) return;
        where.Add($"{column} @{queryKey}");
        parameters.Add(new NpgsqlParameter(queryKey, parsed));
    }
}

public sealed class ValidationException : Exception
{
    public string Code { get; }
    public ValidationException(string code) : base(code) => Code = code;
}

public sealed class DuplicateAeException : Exception
{
    public string AeId { get; }
    public DuplicateAeException(string aeId) => AeId = aeId;
}