package com.ai2dev.springboot116pm.service;

import com.ai2dev.springboot116pm.dto.AdverseEventNotificationDto;
import com.ai2dev.springboot116pm.dto.AdverseEventNotificationPageResponseDto;
import com.ai2dev.springboot116pm.dto.AdverseEventSubmissionRequestDto;
import com.ai2dev.springboot116pm.dto.AdverseEventSubmissionResponseDto;
import com.ai2dev.springboot116pm.model.ActionTaken;
import com.ai2dev.springboot116pm.model.AeAuditLogEntity;
import com.ai2dev.springboot116pm.model.AeNotificationEntity;
import com.ai2dev.springboot116pm.model.AdverseEventEntity;
import com.ai2dev.springboot116pm.model.Outcome;
import com.ai2dev.springboot116pm.repository.AeAuditLogRepository;
import com.ai2dev.springboot116pm.repository.AeNotificationRepository;
import com.ai2dev.springboot116pm.repository.AdverseEventRepository;
import java.time.Duration;
import java.time.Instant;
import java.time.Year;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.dao.DataAccessException;
import org.springframework.data.jdbc.core.JdbcAggregateTemplate;
import org.springframework.data.jdbc.core.dialect.JdbcDialect;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class AdverseEventService
{
    private static final Logger logger = LoggerFactory.getLogger(AdverseEventService.class);

    private final AdverseEventRepository adverseEventRepository;
    private final AeNotificationRepository aeNotificationRepository;
    private final AeAuditLogRepository aeAuditLogRepository;
    private final JdbcTemplate jdbcTemplate;
    private final NamedParameterJdbcTemplate namedParameterJdbcTemplate;

    public AdverseEventService(AdverseEventRepository adverseEventRepository,
            AeNotificationRepository aeNotificationRepository,
            AeAuditLogRepository aeAuditLogRepository,
            JdbcTemplate jdbcTemplate,
            NamedParameterJdbcTemplate namedParameterJdbcTemplate)
    {
        this.adverseEventRepository = adverseEventRepository;
        this.aeNotificationRepository = aeNotificationRepository;
        this.aeAuditLogRepository = aeAuditLogRepository;
        this.jdbcTemplate = jdbcTemplate;
        this.namedParameterJdbcTemplate = namedParameterJdbcTemplate;
    }

    @Transactional
    public AdverseEventSubmissionResponseDto submit(AdverseEventSubmissionRequestDto request)
    {
        logger.info("SERVICE submit adverse event");
        validateRequiredFields(request);
        validateBusinessRules(request);
        AdverseEventSubmissionRequestDto coerced = applyCoercions(request);
        verifyTrialExists(coerced.trialId());
        verifyPatientEnrolled(coerced.trialId(), coerced.patientId());
        DuplicateResult duplicateResult = checkDuplicate(coerced);
        if (duplicateResult.duplicate())
        {
            throw new IllegalStateException("DUPLICATE_AE");
        }
        String aeId = nextAeId();
        String notificationId = nextNotificationId();
        Instant now = Instant.now();
        try
        {
            AdverseEventEntity adverseEventEntity = new AdverseEventEntity(null, aeId, coerced.trialId(), coerced.siteId(), coerced.patientId(), coerced.clinicianId(), coerced.eventDate(), coerced.aeTermCode(), coerced.aeTermName(), coerced.ctcaeGrade(), coerced.serious(), coerced.outcome(), coerced.actionTaken(), coerced.narrative(), coerced.relatedDrugId(), coerced.reportedBy(), now, now, now);
            logger.info("DB INSERT adverse_events");
            adverseEventRepository.save(adverseEventEntity);
            AeNotificationEntity notificationEntity = new AeNotificationEntity(null, notificationId, aeId, coerced.trialId(), coerced.siteId(), coerced.patientId(), coerced.aeTermName(), coerced.ctcaeGrade(), coerced.serious(), coerced.outcome(), priorityForGrade(coerced.ctcaeGrade()), false, null, null, now, now);
            logger.info("DB INSERT ae_notifications");
            aeNotificationRepository.save(notificationEntity);
            AeAuditLogEntity auditLogEntity = new AeAuditLogEntity(null, aeId, "CREATED", coerced.reportedBy(), coerced.serious(), "AE created and notification stored", now);
            logger.info("DB INSERT ae_audit_log");
            aeAuditLogRepository.save(auditLogEntity);
            return new AdverseEventSubmissionResponseDto("success", aeId, notificationId, "Adverse event recorded and notification stored.", now);
        }
        catch (DataAccessException ex)
        {
            logger.error("DB write failed: {}", ex.getMessage());
            throw new IllegalStateException("DB_ERROR");
        }
    }

    public AdverseEventNotificationPageResponseDto getNotifications(String trialId, String siteId, Integer ctcaeGrade, Boolean serious, Boolean acknowledged, String priority, Instant dateFrom, Instant dateTo, Integer page, Integer pageSize)
    {
        logger.info("SERVICE get notifications");
        int resolvedPage = page == null || page < 1 ? 1 : page;
        int resolvedPageSize = pageSize == null ? 20 : pageSize;
        if (resolvedPageSize > 100)
        {
            throw new IllegalArgumentException("PAGE_SIZE_TOO_LARGE");
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
        if (dateFrom != null)
        {
            where.append(" AND created_at >= ?");
            params.add(dateFrom);
        }
        if (dateTo != null)
        {
            where.append(" AND created_at <= ?");
            params.add(dateTo);
        }
        String countSql = "SELECT COUNT(*) FROM ae_notifications" + where;
        logger.info("DB SELECT ae_notifications");
        Long total = jdbcTemplate.queryForObject(countSql, params.toArray(), Long.class);
        String selectSql = "SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, acknowledged_by, acknowledged_at, created_at FROM ae_notifications" + where + " ORDER BY created_at DESC LIMIT ? OFFSET ?";
        params.add(resolvedPageSize);
        params.add((resolvedPage - 1) * resolvedPageSize);
        logger.info("DB SELECT ae_notifications");
        List<AdverseEventNotificationDto> notifications = jdbcTemplate.query(selectSql, params.toArray(), (rs, rowNum) -> new AdverseEventNotificationDto(rs.getString("notification_id"), rs.getString("ae_id"), rs.getString("trial_id"), rs.getString("site_id"), rs.getString("patient_id"), rs.getString("ae_term_name"), rs.getInt("ctcae_grade"), rs.getBoolean("serious"), rs.getString("priority"), rs.getString("outcome"), rs.getBoolean("acknowledged"), rs.getString("acknowledged_by"), rs.getTimestamp("acknowledged_at") == null ? null : rs.getTimestamp("acknowledged_at").toInstant(), rs.getTimestamp("created_at").toInstant()));
        return new AdverseEventNotificationPageResponseDto("success", total == null ? 0L : total, resolvedPage, resolvedPageSize, notifications);
    }

    private void validateRequiredFields(AdverseEventSubmissionRequestDto request)
    {
        if (isBlank(request.trialId()) || isBlank(request.siteId()) || isBlank(request.patientId()) || isBlank(request.clinicianId()) || request.eventDate() == null || isBlank(request.aeTermCode()) || isBlank(request.aeTermName()) || request.ctcaeGrade() == null || request.serious() == null || request.outcome() == null || request.actionTaken() == null || isBlank(request.narrative()) || isBlank(request.reportedBy()))
        {
            logger.error("Validation failed: MISSING_REQUIRED_FIELD");
            throw new IllegalArgumentException("MISSING_REQUIRED_FIELD");
        }
    }

    private void validateBusinessRules(AdverseEventSubmissionRequestDto request)
    {
        if (request.ctcaeGrade() < 1 || request.ctcaeGrade() > 5)
        {
            logger.error("Validation failed: INVALID_CTCAE_GRADE");
            throw new IllegalArgumentException("INVALID_CTCAE_GRADE");
        }
        if (request.outcome() == null)
        {
            logger.error("Validation failed: INVALID_OUTCOME");
            throw new IllegalArgumentException("INVALID_OUTCOME");
        }
        if (request.actionTaken() == null)
        {
            logger.error("Validation failed: INVALID_ACTION_TAKEN");
            throw new IllegalArgumentException("INVALID_ACTION_TAKEN");
        }
        if (request.narrative().length() > 2000)
        {
            logger.error("Validation failed: NARRATIVE_TOO_LONG");
            throw new IllegalArgumentException("NARRATIVE_TOO_LONG");
        }
    }

    private AdverseEventSubmissionRequestDto applyCoercions(AdverseEventSubmissionRequestDto request)
    {
        boolean serious = request.ctcaeGrade() >= 3 || request.serious();
        Outcome outcome = request.ctcaeGrade() == 5 ? Outcome.FATAL : request.outcome();
        return new AdverseEventSubmissionRequestDto(request.trialId(), request.siteId(), request.patientId(), request.clinicianId(), request.eventDate(), request.aeTermCode(), request.aeTermName(), request.ctcaeGrade(), serious, outcome, request.actionTaken(), request.narrative(), request.relatedDrugId(), request.reportedBy());
    }

    private void verifyTrialExists(String trialId)
    {
        logger.info("DB SELECT trials");
        Integer count = jdbcTemplate.queryForObject("SELECT COUNT(*) FROM trials WHERE trial_id = ? AND status = 'ACTIVE'", Integer.class, trialId);
        if (count == null || count == 0)
        {
            throw new IllegalArgumentException("TRIAL_NOT_FOUND");
        }
    }

    private void verifyPatientEnrolled(String trialId, String patientId)
    {
        logger.info("DB SELECT trial_enrolments");
        Integer count = jdbcTemplate.queryForObject("SELECT COUNT(*) FROM trial_enrolments WHERE trial_id = ? AND patient_id = ? AND status = 'ENROLLED'", Integer.class, trialId, patientId);
        if (count == null || count == 0)
        {
            throw new IllegalArgumentException("PATIENT_NOT_FOUND");
        }
    }

    private DuplicateResult checkDuplicate(AdverseEventSubmissionRequestDto request)
    {
        logger.info("DB SELECT adverse_events");
        Instant from = request.eventDate().minus(Duration.ofSeconds(60));
        Integer count = jdbcTemplate.queryForObject("SELECT COUNT(*) FROM adverse_events WHERE trial_id = ? AND patient_id = ? AND ae_term_code = ? AND submitted_at >= ?", Integer.class, request.trialId(), request.patientId(), request.aeTermCode(), from);
        return new DuplicateResult(count != null && count > 0);
    }

    private String nextAeId()
    {
        Integer seq = jdbcTemplate.queryForObject("SELECT nextval('ae_id_seq')", Integer.class);
        return "AE-" + Year.now().getValue() + "-" + String.format("%06d", Objects.requireNonNull(seq));
    }

    private String nextNotificationId()
    {
        Integer seq = jdbcTemplate.queryForObject("SELECT nextval('notif_id_seq')", Integer.class);
        return "NOTIF-" + Year.now().getValue() + "-" + String.format("%06d", Objects.requireNonNull(seq));
    }

    private String priorityForGrade(int grade)
    {
        return grade >= 3 ? "HIGH" : "NORMAL";
    }

    private boolean isBlank(String value)
    {
        return value == null || value.isBlank();
    }

    private record DuplicateResult(boolean duplicate)
    {
    }
}