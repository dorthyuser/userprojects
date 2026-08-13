package com.ai2dev.springboot1111.service;

import com.ai2dev.springboot1111.dto.AdverseEventNotificationResponseDto;
import com.ai2dev.springboot1111.dto.AdverseEventRequestDto;
import com.ai2dev.springboot1111.dto.AdverseEventResponseDto;
import com.ai2dev.springboot1111.dto.NotificationSearchResponseDto;
import com.ai2dev.springboot1111.model.ActionTaken;
import com.ai2dev.springboot1111.model.AdverseEventEntity;
import com.ai2dev.springboot1111.model.AdverseEventNotificationEntity;
import com.ai2dev.springboot1111.model.AeOutcome;
import com.ai2dev.springboot1111.model.AuditAction;
import com.ai2dev.springboot1111.model.Priority;
import com.ai2dev.springboot1111.repository.AdverseEventAuditLogRepository;
import com.ai2dev.springboot1111.repository.AdverseEventNotificationRepository;
import com.ai2dev.springboot1111.repository.AdverseEventRepository;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.time.Instant;
import java.time.ZoneOffset;
import java.time.ZonedDateTime;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.dao.DataAccessException;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.namedparam.MapSqlParameterSource;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class AdverseEventService
{
    private static final Logger logger = LoggerFactory.getLogger(AdverseEventService.class);

    private final JdbcTemplate jdbcTemplate;
    private final NamedParameterJdbcTemplate namedParameterJdbcTemplate;
    private final AdverseEventRepository adverseEventRepository;
    private final AdverseEventNotificationRepository adverseEventNotificationRepository;
    private final AdverseEventAuditLogRepository adverseEventAuditLogRepository;

    public AdverseEventService(JdbcTemplate jdbcTemplate,
            NamedParameterJdbcTemplate namedParameterJdbcTemplate,
            AdverseEventRepository adverseEventRepository,
            AdverseEventNotificationRepository adverseEventNotificationRepository,
            AdverseEventAuditLogRepository adverseEventAuditLogRepository)
    {
        this.jdbcTemplate = jdbcTemplate;
        this.namedParameterJdbcTemplate = namedParameterJdbcTemplate;
        this.adverseEventRepository = adverseEventRepository;
        this.adverseEventNotificationRepository = adverseEventNotificationRepository;
        this.adverseEventAuditLogRepository = adverseEventAuditLogRepository;
    }

    @Transactional
    public AdverseEventResponseDto submitAdverseEvent(AdverseEventRequestDto request)
    {
        logger.info("service=submitAdverseEvent resource=adverse_event step=VALIDATION");
        validateRequest(request);
        int grade = request.ctcaeGrade();
        boolean serious = grade >= 3 || Boolean.TRUE.equals(request.serious());
        AeOutcome outcome = grade == 5 ? AeOutcome.FATAL : request.outcome();
        Priority priority = grade >= 3 ? Priority.HIGH : Priority.NORMAL;

        verifyTrialExists(request.trialId());
        verifyPatientEnrolled(request.trialId(), request.patientId());
        String existingAeId = findDuplicateAeId(request.trialId(), request.patientId(), request.aeTermCode(), request.eventDate());
        if (existingAeId != null)
        {
            throw new IllegalStateException("DUPLICATE_AE");
        }

        String aeId = generateAeId();
        String notificationId = generateNotificationId();
        Instant receivedAt = Instant.now();

        try
        {
            logger.info("table=adverse_events operation=INSERT");
            AdverseEventEntity savedAe = adverseEventRepository.save(new AdverseEventEntity(null, aeId, request.trialId(), request.siteId(), request.patientId(), request.clinicianId(), request.eventDate(), request.aeTermCode(), request.aeTermName(), grade, serious, outcome, request.actionTaken(), request.narrative(), request.relatedDrugId(), request.reportedBy(), receivedAt, receivedAt, receivedAt));

            logger.info("table=ae_notifications operation=INSERT");
            AdverseEventNotificationEntity savedNotification = adverseEventNotificationRepository.save(new AdverseEventNotificationEntity(null, notificationId, savedAe.aeId(), request.trialId(), request.siteId(), request.patientId(), request.aeTermName(), grade, serious, outcome, priority, false, null, null, receivedAt, receivedAt));

            logger.info("table=ae_audit_log operation=INSERT");
            adverseEventAuditLogRepository.save(new com.ai2dev.springboot1111.model.AdverseEventAuditLogEntity(null, savedAe.aeId(), AuditAction.CREATED, request.reportedBy(), serious, "AE created", receivedAt));

            return new AdverseEventResponseDto("success", savedAe.aeId(), savedNotification.notificationId(), "Adverse event recorded and notification stored.", receivedAt);
        }
        catch (DataAccessException ex)
        {
            logger.error("DB_ERROR: {}", ex.getMessage());
            throw new IllegalStateException("DB_ERROR");
        }
    }

    public NotificationSearchResponseDto getNotifications(String trialId, String siteId, Integer ctcaeGrade, Boolean serious, Boolean acknowledged, String priority, Instant dateFrom, Instant dateTo, Integer page, Integer pageSize)
    {
        logger.info("service=getNotifications resource=notification step=READ");
        if (pageSize != null && pageSize > 100)
        {
            throw new IllegalArgumentException("INVALID_PAGE_SIZE");
        }
        StringBuilder sql = new StringBuilder("SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, acknowledged_by, acknowledged_at, created_at FROM ae_notifications WHERE 1=1");
        MapSqlParameterSource params = new MapSqlParameterSource();
        if (trialId != null && !trialId.isBlank())
        {
            sql.append(" AND trial_id = :trialId");
            params.addValue("trialId", trialId);
        }
        if (siteId != null && !siteId.isBlank())
        {
            sql.append(" AND site_id = :siteId");
            params.addValue("siteId", siteId);
        }
        if (ctcaeGrade != null)
        {
            sql.append(" AND ctcae_grade = :ctcaeGrade");
            params.addValue("ctcaeGrade", ctcaeGrade);
        }
        if (serious != null)
        {
            sql.append(" AND serious = :serious");
            params.addValue("serious", serious);
        }
        if (acknowledged != null)
        {
            sql.append(" AND acknowledged = :acknowledged");
            params.addValue("acknowledged", acknowledged);
        }
        if (priority != null && !priority.isBlank())
        {
            sql.append(" AND priority = :priority");
            params.addValue("priority", priority);
        }
        if (dateFrom != null)
        {
            sql.append(" AND created_at >= :dateFrom");
            params.addValue("dateFrom", dateFrom);
        }
        if (dateTo != null)
        {
            sql.append(" AND created_at <= :dateTo");
            params.addValue("dateTo", dateTo);
        }
        String countSql = "SELECT COUNT(*) FROM ae_notifications WHERE 1=1" + sql.substring(sql.indexOf(" AND"));
        long total = namedParameterJdbcTemplate.queryForObject(countSql, params, Long.class);
        int effectivePage = page == null ? 1 : page;
        int effectivePageSize = pageSize == null ? 20 : pageSize;
        sql.append(" ORDER BY created_at DESC LIMIT :limit OFFSET :offset");
        params.addValue("limit", effectivePageSize);
        params.addValue("offset", (effectivePage - 1L) * effectivePageSize);
        List<AdverseEventNotificationResponseDto> notifications = namedParameterJdbcTemplate.query(sql.toString(), params, (rs, rowNum) -> mapNotification(rs));
        return new NotificationSearchResponseDto("success", total, effectivePage, effectivePageSize, notifications);
    }

    private void validateRequest(AdverseEventRequestDto request)
    {
        if (request == null)
        {
            throw new IllegalArgumentException("MISSING_REQUIRED_FIELD");
        }
        if (isBlank(request.trialId()) || isBlank(request.siteId()) || isBlank(request.patientId()) || isBlank(request.clinicianId()) || request.eventDate() == null || isBlank(request.aeTermCode()) || isBlank(request.aeTermName()) || request.ctcaeGrade() == null || request.serious() == null || request.outcome() == null || request.actionTaken() == null || isBlank(request.narrative()) || isBlank(request.reportedBy()))
        {
            throw new IllegalArgumentException("MISSING_REQUIRED_FIELD");
        }
        if (request.ctcaeGrade() < 1 || request.ctcaeGrade() > 5)
        {
            throw new IllegalArgumentException("INVALID_CTCAE_GRADE");
        }
        if (request.outcome() == null)
        {
            throw new IllegalArgumentException("INVALID_OUTCOME");
        }
        if (request.actionTaken() == null)
        {
            throw new IllegalArgumentException("INVALID_ACTION_TAKEN");
        }
        if (request.narrative().length() > 2000)
        {
            throw new IllegalArgumentException("NARRATIVE_TOO_LONG");
        }
    }

    private void verifyTrialExists(String trialId)
    {
        logger.info("table=trials operation=SELECT");
        Integer id = jdbcTemplate.query("SELECT id FROM trials WHERE trial_id = ? AND status = 'ACTIVE'", rs -> rs.next() ? rs.getInt("id") : null, trialId);
        if (id == null)
        {
            throw new com.ai2dev.springboot1111.exception.EntityNotFoundException("TRIAL_NOT_FOUND");
        }
    }

    private void verifyPatientEnrolled(String trialId, String patientId)
    {
        logger.info("table=trial_enrolments operation=SELECT");
        Integer id = jdbcTemplate.query("SELECT id FROM trial_enrolments WHERE trial_id = ? AND patient_id = ? AND status = 'ENROLLED'", rs -> rs.next() ? rs.getInt("id") : null, trialId, patientId);
        if (id == null)
        {
            throw new com.ai2dev.springboot1111.exception.EntityNotFoundException("PATIENT_NOT_FOUND");
        }
    }

    private String findDuplicateAeId(String trialId, String patientId, String aeTermCode, Instant eventDate)
    {
        logger.info("table=adverse_events operation=SELECT");
        List<String> ids = jdbcTemplate.query("SELECT ae_id FROM adverse_events WHERE trial_id = ? AND patient_id = ? AND ae_term_code = ? AND submitted_at >= ?", (rs, rowNum) -> rs.getString("ae_id"), trialId, patientId, aeTermCode, eventDate.minusSeconds(60));
        return ids.isEmpty() ? null : ids.get(0);
    }

    private String generateAeId()
    {
        Integer seq = jdbcTemplate.queryForObject("SELECT nextval('ae_id_seq')", Integer.class);
        int year = ZonedDateTime.now(ZoneOffset.UTC).getYear();
        return String.format("AE-%d-%06d", year, seq);
    }

    private String generateNotificationId()
    {
        Integer seq = jdbcTemplate.queryForObject("SELECT nextval('notif_id_seq')", Integer.class);
        int year = ZonedDateTime.now(ZoneOffset.UTC).getYear();
        return String.format("NOTIF-%d-%06d", year, seq);
    }

    private AdverseEventNotificationResponseDto mapNotification(ResultSet rs) throws SQLException
    {
        return new AdverseEventNotificationResponseDto(rs.getString("notification_id"), rs.getString("ae_id"), rs.getString("trial_id"), rs.getString("site_id"), rs.getString("patient_id"), rs.getString("ae_term_name"), rs.getInt("ctcae_grade"), rs.getBoolean("serious"), rs.getString("priority"), rs.getString("outcome"), rs.getBoolean("acknowledged"), rs.getString("acknowledged_by"), rs.getTimestamp("acknowledged_at") == null ? null : rs.getTimestamp("acknowledged_at").toInstant(), rs.getTimestamp("created_at").toInstant());
    }

    private boolean isBlank(String value)
    {
        return value == null || value.isBlank();
    }
}
