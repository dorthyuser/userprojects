package com.ai2dev.springboot258pm.service;

import com.ai2dev.springboot258pm.dto.AdverseEventNotificationResponseDto;
import com.ai2dev.springboot258pm.dto.AdverseEventRequestDto;
import com.ai2dev.springboot258pm.dto.AdverseEventResponseDto;
import com.ai2dev.springboot258pm.dto.NotificationSearchResponseDto;
import com.ai2dev.springboot258pm.model.AdverseEventEntity;
import com.ai2dev.springboot258pm.model.AdverseEventNotificationEntity;
import com.ai2dev.springboot258pm.model.AdverseEventAuditLogEntity;
import com.ai2dev.springboot258pm.model.ActionTaken;
import com.ai2dev.springboot258pm.model.Outcome;
import com.ai2dev.springboot258pm.model.Priority;
import com.ai2dev.springboot258pm.repository.AdverseEventAuditLogRepository;
import com.ai2dev.springboot258pm.repository.AdverseEventNotificationRepository;
import com.ai2dev.springboot258pm.repository.AdverseEventRepository;
import java.time.Instant;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.dao.DataAccessException;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class AdverseEventService
{
    private static final Logger logger = LoggerFactory.getLogger(AdverseEventService.class);

    private final AdverseEventRepository adverseEventRepository;
    private final AdverseEventNotificationRepository adverseEventNotificationRepository;
    private final AdverseEventAuditLogRepository adverseEventAuditLogRepository;
    private final JdbcTemplate jdbcTemplate;
    private final NamedParameterJdbcTemplate namedParameterJdbcTemplate;

    public AdverseEventService(AdverseEventRepository adverseEventRepository,
            AdverseEventNotificationRepository adverseEventNotificationRepository,
            AdverseEventAuditLogRepository adverseEventAuditLogRepository,
            JdbcTemplate jdbcTemplate,
            NamedParameterJdbcTemplate namedParameterJdbcTemplate)
    {
        this.adverseEventRepository = adverseEventRepository;
        this.adverseEventNotificationRepository = adverseEventNotificationRepository;
        this.adverseEventAuditLogRepository = adverseEventAuditLogRepository;
        this.jdbcTemplate = jdbcTemplate;
        this.namedParameterJdbcTemplate = namedParameterJdbcTemplate;
    }

    @Transactional
    public AdverseEventResponseDto submitAdverseEvent(AdverseEventRequestDto request)
    {
        logger.info("Start submit adverse event");
        validateRequest(request);
        int grade = request.ctcaeGrade();
        boolean serious = grade >= 3 || request.serious();
        Outcome outcome = grade == 5 ? Outcome.FATAL : request.outcome();
        Priority priority = grade >= 3 ? Priority.HIGH : Priority.NORMAL;
        Instant eventInstant = request.eventDate().toInstant();
        verifyTrialExists(request.trialId());
        verifyPatientEnrolled(request.trialId(), request.patientId());
        String duplicateAeId = findDuplicateAeId(request.trialId(), request.patientId(), request.aeTermCode(), eventInstant);
        if (duplicateAeId != null)
        {
            throw new IllegalStateException("DUPLICATE_AE");
        }
        String aeId = generateAeId();
        String notificationId = generateNotificationId();
        try
        {
            AdverseEventEntity savedAe = adverseEventRepository.save(new AdverseEventEntity(null, aeId, request.trialId(), request.siteId(), request.patientId(), request.clinicianId(), request.eventDate(), request.aeTermCode(), request.aeTermName(), grade, serious, outcome, request.actionTaken(), request.narrative(), request.relatedDrugId(), request.reportedBy(), OffsetDateTime.now(ZoneOffset.UTC), OffsetDateTime.now(ZoneOffset.UTC), OffsetDateTime.now(ZoneOffset.UTC)));
            adverseEventNotificationRepository.save(new AdverseEventNotificationEntity(null, notificationId, savedAe.aeId(), request.trialId(), request.siteId(), request.patientId(), request.aeTermName(), grade, serious, outcome, priority, false, null, null, OffsetDateTime.now(ZoneOffset.UTC), OffsetDateTime.now(ZoneOffset.UTC)));
            adverseEventAuditLogRepository.save(new AdverseEventAuditLogEntity(null, savedAe.aeId(), "CREATED", request.reportedBy(), serious, "AE created", OffsetDateTime.now(ZoneOffset.UTC)));
            return new AdverseEventResponseDto("success", savedAe.aeId(), notificationId, "Adverse event recorded and notification stored.", OffsetDateTime.now(ZoneOffset.UTC).toString());
        }
        catch (DataAccessException ex)
        {
            logger.error("DB_ERROR", ex);
            throw new IllegalStateException("DB_ERROR");
        }
    }

    public NotificationSearchResponseDto getNotifications(String trialId, String siteId, Integer ctcaeGrade, Boolean serious, Boolean acknowledged, String priority, String dateFrom, String dateTo, int page, int pageSize)
    {
        logger.info("Start get notifications");
        if (pageSize > 100)
        {
            throw new IllegalArgumentException("PAGE_SIZE_EXCEEDED");
        }
        StringBuilder where = new StringBuilder(" WHERE 1=1");
        List<Object> params = new ArrayList<>();
        if (trialId != null && !trialId.isBlank())
        {
            where.append(" AND trial_id = ?");
            params.add(trialId);
        }
        if (siteId != null && !siteId.isBlank())
        {
            where.append(" AND site_id = ?");
            params.add(siteId);
        }
        if (ctcaeGrade != null)
        {
            where.append(" AND ctcae_grade = ?");
            params.add(ctcaeGrade);
        }
        if (serious != null)
        {
            where.append(" AND serious = ?");
            params.add(serious);
        }
        if (acknowledged != null)
        {
            where.append(" AND acknowledged = ?");
            params.add(acknowledged);
        }
        if (priority != null && !priority.isBlank())
        {
            where.append(" AND priority = ?");
            params.add(priority);
        }
        if (dateFrom != null && !dateFrom.isBlank())
        {
            where.append(" AND created_at >= ?");
            params.add(OffsetDateTime.parse(dateFrom));
        }
        if (dateTo != null && !dateTo.isBlank())
        {
            where.append(" AND created_at <= ?");
            params.add(OffsetDateTime.parse(dateTo));
        }
        Long total = jdbcTemplate.queryForObject("SELECT COUNT(*) FROM ae_notifications" + where, params.toArray(), Long.class);
        String sql = "SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, acknowledged_by, acknowledged_at, created_at FROM ae_notifications" + where + " ORDER BY created_at DESC LIMIT ? OFFSET ?";
        params.add(pageSize);
        params.add((page - 1) * pageSize);
        List<AdverseEventNotificationResponseDto> notifications = jdbcTemplate.query(sql, params.toArray(), (rs, rowNum) -> new AdverseEventNotificationResponseDto(rs.getString("notification_id"), rs.getString("ae_id"), rs.getString("trial_id"), rs.getString("site_id"), rs.getString("patient_id"), rs.getString("ae_term_name"), rs.getInt("ctcae_grade"), rs.getBoolean("serious"), rs.getString("priority"), rs.getString("outcome"), rs.getBoolean("acknowledged"), rs.getString("acknowledged_by"), rs.getObject("acknowledged_at", OffsetDateTime.class), rs.getObject("created_at", OffsetDateTime.class)));
        return new NotificationSearchResponseDto("success", total == null ? 0L : total, page, pageSize, notifications);
    }

    private void validateRequest(AdverseEventRequestDto request)
    {
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
        if (request.narrative() != null && request.narrative().length() > 2000)
        {
            throw new IllegalArgumentException("NARRATIVE_TOO_LONG");
        }
    }

    private void verifyTrialExists(String trialId)
    {
        Integer id = jdbcTemplate.queryForObject("SELECT id FROM trials WHERE trial_id = ? AND status = 'ACTIVE'", Integer.class, trialId);
        if (id == null)
        {
            throw new IllegalArgumentException("TRIAL_NOT_FOUND");
        }
    }

    private void verifyPatientEnrolled(String trialId, String patientId)
    {
        Integer id = jdbcTemplate.queryForObject("SELECT id FROM trial_enrolments WHERE trial_id = ? AND patient_id = ? AND status = 'ENROLLED'", Integer.class, trialId, patientId);
        if (id == null)
        {
            throw new IllegalArgumentException("PATIENT_NOT_FOUND");
        }
    }

    private String findDuplicateAeId(String trialId, String patientId, String aeTermCode, Instant eventInstant)
    {
        List<String> ids = jdbcTemplate.query("SELECT ae_id FROM adverse_events WHERE trial_id = ? AND patient_id = ? AND ae_term_code = ? AND submitted_at >= ?", (rs, rowNum) -> rs.getString("ae_id"), trialId, patientId, aeTermCode, OffsetDateTime.ofInstant(eventInstant.minusSeconds(60), ZoneOffset.UTC));
        return ids.isEmpty() ? null : ids.get(0);
    }

    private String generateAeId()
    {
        Long seq = jdbcTemplate.queryForObject("SELECT nextval('ae_id_seq')", Long.class);
        return "AE-" + OffsetDateTime.now(ZoneOffset.UTC).getYear() + "-" + String.format("%06d", seq == null ? 0L : seq);
    }

    private String generateNotificationId()
    {
        Long seq = jdbcTemplate.queryForObject("SELECT nextval('notif_id_seq')", Long.class);
        return "NOTIF-" + OffsetDateTime.now(ZoneOffset.UTC).getYear() + "-" + String.format("%06d", seq == null ? 0L : seq);
    }
}