package com.ai2dev.springbootae1019.service;

import com.ai2dev.springbootae1019.dto.AdverseEventNotificationDto;
import com.ai2dev.springbootae1019.dto.AdverseEventNotificationPageResponseDto;
import com.ai2dev.springbootae1019.dto.AdverseEventRequestDto;
import com.ai2dev.springbootae1019.dto.AdverseEventResponseDto;
import com.ai2dev.springbootae1019.dto.NotificationFilterRequestDto;
import com.ai2dev.springbootae1019.exception.EntityNotFoundException;
import com.ai2dev.springbootae1019.model.ActionTaken;
import com.ai2dev.springbootae1019.model.AeAuditLogEntity;
import com.ai2dev.springbootae1019.model.AeNotificationEntity;
import com.ai2dev.springbootae1019.model.AdverseEventEntity;
import com.ai2dev.springbootae1019.model.Outcome;
import com.ai2dev.springbootae1019.repository.AeAuditLogRepository;
import com.ai2dev.springbootae1019.repository.AeNotificationRepository;
import com.ai2dev.springbootae1019.repository.AdverseEventRepository;
import java.sql.Timestamp;
import java.time.Instant;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
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

    private final AeAuditLogRepository aeAuditLogRepository;
    private final AeNotificationRepository aeNotificationRepository;
    private final AdverseEventRepository adverseEventRepository;
    private final JdbcTemplate jdbcTemplate;
    private final NamedParameterJdbcTemplate namedParameterJdbcTemplate;

    public AdverseEventService(AdverseEventRepository adverseEventRepository, AeNotificationRepository aeNotificationRepository, AeAuditLogRepository aeAuditLogRepository, JdbcTemplate jdbcTemplate, NamedParameterJdbcTemplate namedParameterJdbcTemplate)
    {
        this.adverseEventRepository = adverseEventRepository;
        this.aeNotificationRepository = aeNotificationRepository;
        this.aeAuditLogRepository = aeAuditLogRepository;
        this.jdbcTemplate = jdbcTemplate;
        this.namedParameterJdbcTemplate = namedParameterJdbcTemplate;
    }

    @Transactional
    public AdverseEventResponseDto submitAdverseEvent(AdverseEventRequestDto requestDto)
    {
        logger.info("Service submitAdverseEvent start");
        validateBusinessRules(requestDto);
        AdverseEventRequestDto coerced = applyCoercions(requestDto);
        verifyTrialExists(coerced.trialId());
        verifyPatientEnrolled(coerced.trialId(), coerced.patientId());
        DuplicateResult duplicateResult = findDuplicate(coerced);
        if (duplicateResult != null)
        {
            throw new IllegalArgumentException("DUPLICATE_AE");
        }
        String aeId = generateAeId();
        String notificationId = generateNotificationId();
        Instant now = Instant.now();
        try
        {
            AdverseEventEntity savedAe = adverseEventRepository.save(new AdverseEventEntity(null, aeId, coerced.trialId(), coerced.siteId(), coerced.patientId(), coerced.clinicianId(), coerced.eventDate(), coerced.aeTermCode(), coerced.aeTermName(), coerced.ctcaeGrade(), coerced.serious(), coerced.outcome(), coerced.actionTaken(), coerced.narrative(), coerced.relatedDrugId(), coerced.reportedBy(), Timestamp.from(now), Timestamp.from(now), Timestamp.from(now)));
            AeNotificationEntity savedNotification = aeNotificationRepository.save(new AeNotificationEntity(null, notificationId, savedAe.aeId(), coerced.trialId(), coerced.siteId(), coerced.patientId(), coerced.aeTermName(), coerced.ctcaeGrade(), coerced.serious(), coerced.outcome(), coerced.ctcaeGrade() >= 3 ? "HIGH" : "NORMAL", false, null, null, Timestamp.from(now), Timestamp.from(now)));
            aeAuditLogRepository.save(new AeAuditLogEntity(null, savedAe.aeId(), "CREATED", coerced.reportedBy(), coerced.serious(), "AE created", Timestamp.from(now)));
            return new AdverseEventResponseDto("success", savedAe.aeId(), savedNotification.notificationId(), "Adverse event recorded and notification stored.", OffsetDateTime.ofInstant(now, ZoneOffset.UTC).toString());
        }
        catch (DataAccessException ex)
        {
            logger.error("DB write failed: {}", ex.getMessage());
            throw new IllegalStateException("DB_ERROR");
        }
    }

    public AdverseEventNotificationPageResponseDto getNotifications(NotificationFilterRequestDto filter)
    {
        logger.info("Service getNotifications start");
        int page = filter.page() == null ? 1 : filter.page();
        int pageSize = filter.pageSize() == null ? 20 : filter.pageSize();
        if (pageSize > 100)
        {
            throw new IllegalArgumentException("pageSize must not exceed 100");
        }
        StringBuilder where = new StringBuilder(" WHERE 1=1");
        MapSqlBuilder builder = new MapSqlBuilder();
        if (filter.trialId() != null && !filter.trialId().isBlank())
        {
            where.append(" AND trial_id = :trialId");
            builder.put("trialId", filter.trialId());
        }
        if (filter.siteId() != null && !filter.siteId().isBlank())
        {
            where.append(" AND site_id = :siteId");
            builder.put("siteId", filter.siteId());
        }
        if (filter.ctcaeGrade() != null)
        {
            where.append(" AND ctcae_grade = :ctcaeGrade");
            builder.put("ctcaeGrade", filter.ctcaeGrade());
        }
        if (filter.serious() != null)
        {
            where.append(" AND serious = :serious");
            builder.put("serious", filter.serious());
        }
        if (filter.acknowledged() != null)
        {
            where.append(" AND acknowledged = :acknowledged");
            builder.put("acknowledged", filter.acknowledged());
        }
        if (filter.priority() != null && !filter.priority().isBlank())
        {
            where.append(" AND priority = :priority");
            builder.put("priority", filter.priority());
        }
        if (filter.dateFrom() != null && !filter.dateFrom().isBlank())
        {
            where.append(" AND created_at >= :dateFrom");
            builder.put("dateFrom", OffsetDateTime.parse(filter.dateFrom()));
        }
        if (filter.dateTo() != null && !filter.dateTo().isBlank())
        {
            where.append(" AND created_at <= :dateTo");
            builder.put("dateTo", OffsetDateTime.parse(filter.dateTo()));
        }
        long total = namedParameterJdbcTemplate.queryForObject("SELECT COUNT(*) FROM ae_notifications" + where, builder.params(), Long.class);
        List<AdverseEventNotificationDto> notifications = namedParameterJdbcTemplate.query("SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, acknowledged_by, acknowledged_at, created_at FROM ae_notifications" + where + " ORDER BY created_at DESC LIMIT :limit OFFSET :offset", builder.withPaging(pageSize, (page - 1) * pageSize), (rs, rowNum) -> new AdverseEventNotificationDto(rs.getString("notification_id"), rs.getString("ae_id"), rs.getString("trial_id"), rs.getString("site_id"), rs.getString("patient_id"), rs.getString("ae_term_name"), rs.getInt("ctcae_grade"), rs.getBoolean("serious"), rs.getString("priority"), rs.getString("outcome"), rs.getBoolean("acknowledged"), rs.getString("acknowledged_by"), rs.getTimestamp("acknowledged_at") == null ? null : rs.getTimestamp("acknowledged_at").toInstant().toString(), rs.getTimestamp("created_at").toInstant().toString()));
        return new AdverseEventNotificationPageResponseDto("success", total, page, pageSize, notifications);
    }

    private void validateBusinessRules(AdverseEventRequestDto requestDto)
    {
        if (requestDto.ctcaeGrade() == null || requestDto.ctcaeGrade() < 1 || requestDto.ctcaeGrade() > 5)
        {
            throw new IllegalArgumentException("INVALID_CTCAE_GRADE");
        }
        if (requestDto.outcome() == null)
        {
            throw new IllegalArgumentException("INVALID_OUTCOME");
        }
        if (requestDto.actionTaken() == null)
        {
            throw new IllegalArgumentException("INVALID_ACTION_TAKEN");
        }
        if (requestDto.narrative() != null && requestDto.narrative().length() > 2000)
        {
            throw new IllegalArgumentException("NARRATIVE_TOO_LONG");
        }
    }

    private AdverseEventRequestDto applyCoercions(AdverseEventRequestDto requestDto)
    {
        boolean serious = requestDto.ctcaeGrade() >= 3;
        Outcome outcome = requestDto.ctcaeGrade() == 5 ? Outcome.FATAL : requestDto.outcome();
        return new AdverseEventRequestDto(requestDto.trialId(), requestDto.siteId(), requestDto.patientId(), requestDto.clinicianId(), requestDto.eventDate(), requestDto.aeTermCode(), requestDto.aeTermName(), requestDto.ctcaeGrade(), serious, outcome, requestDto.actionTaken(), requestDto.narrative(), requestDto.relatedDrugId(), requestDto.reportedBy());
    }

    private void verifyTrialExists(String trialId)
    {
        Integer id = jdbcTemplate.query("SELECT id FROM trials WHERE trial_id = ? AND status = 'ACTIVE'", rs -> rs.next() ? rs.getInt("id") : null, trialId);
        if (id == null)
        {
            throw new EntityNotFoundException("TRIAL_NOT_FOUND");
        }
    }

    private void verifyPatientEnrolled(String trialId, String patientId)
    {
        Integer id = jdbcTemplate.query("SELECT id FROM trial_enrolments WHERE trial_id = ? AND patient_id = ? AND status = 'ENROLLED'", rs -> rs.next() ? rs.getInt("id") : null, trialId, patientId);
        if (id == null)
        {
            throw new EntityNotFoundException("PATIENT_NOT_FOUND");
        }
    }

    private DuplicateResult findDuplicate(AdverseEventRequestDto requestDto)
    {
        List<DuplicateResult> results = jdbcTemplate.query("SELECT ae_id FROM adverse_events WHERE trial_id = ? AND patient_id = ? AND ae_term_code = ? AND submitted_at >= NOW() - INTERVAL '60 seconds'", (rs, rowNum) -> new DuplicateResult(rs.getString("ae_id")), requestDto.trialId(), requestDto.patientId(), requestDto.aeTermCode());
        return results.isEmpty() ? null : results.get(0);
    }

    private String generateAeId()
    {
        Long seq = jdbcTemplate.queryForObject("SELECT nextval('ae_id_seq')", Long.class);
        return "AE-" + java.time.Year.now().getValue() + "-" + String.format("%06d", seq);
    }

    private String generateNotificationId()
    {
        Long seq = jdbcTemplate.queryForObject("SELECT nextval('notif_id_seq')", Long.class);
        return "NOTIF-" + java.time.Year.now().getValue() + "-" + String.format("%06d", seq);
    }

    private static final class DuplicateResult
    {
        private final String aeId;

        private DuplicateResult(String aeId)
        {
            this.aeId = aeId;
        }
    }

    private static final class MapSqlBuilder
    {
        private final java.util.Map<String, Object> params = new java.util.HashMap<>();

        private void put(String key, Object value)
        {
            params.put(key, value);
        }

        private java.util.Map<String, Object> params()
        {
            return params;
        }

        private java.util.Map<String, Object> withPaging(int limit, int offset)
        {
            params.put("limit", limit);
            params.put("offset", offset);
            return params;
        }
    }
}
