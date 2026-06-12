package com.ai2dev.springbootaetest620.service;

import com.ai2dev.springbootaetest620.exception.EntityNotFoundException;
import com.ai2dev.springbootaetest620.model.AdverseEventEntity;
import com.ai2dev.springbootaetest620.model.AdverseEventNotificationEntity;
import com.ai2dev.springbootaetest620.model.AdverseEventOutcome;
import com.ai2dev.springbootaetest620.model.AdverseEventPriority;
import com.ai2dev.springbootaetest620.model.ActionTaken;
import com.ai2dev.springbootaetest620.model.dto.AdverseEventNotificationItemDto;
import com.ai2dev.springbootaetest620.model.dto.AdverseEventNotificationResponseDto;
import com.ai2dev.springbootaetest620.model.dto.AdverseEventRequestDto;
import com.ai2dev.springbootaetest620.model.dto.AdverseEventResponseDto;
import com.ai2dev.springbootaetest620.model.dto.NotificationQueryDto;
import java.sql.Timestamp;
import java.time.Instant;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.dao.DataAccessException;
import org.springframework.jdbc.core.namedparam.MapSqlParameterSource;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class AdverseEventService
{
    private static final Logger logger = LoggerFactory.getLogger(AdverseEventService.class);

    private final NamedParameterJdbcTemplate namedParameterJdbcTemplate;

    public AdverseEventService(NamedParameterJdbcTemplate namedParameterJdbcTemplate)
    {
        this.namedParameterJdbcTemplate = namedParameterJdbcTemplate;
    }

    @Transactional
    public AdverseEventResponseDto submit(AdverseEventRequestDto request)
    {
        logger.info("Service entry: submit adverse event");
        AdverseEventRequestDto coerced = coerce(request);
        validateBusinessRules(coerced);
        try
        {
            logger.info("DB operation: SELECT trials");
            Integer trialExists = namedParameterJdbcTemplate.query("SELECT 1 FROM trials WHERE trial_id = :trialId AND status = 'ACTIVE' LIMIT 1", new MapSqlParameterSource("trialId", coerced.trialId()), rs -> rs.next() ? 1 : null);
            if (trialExists == null)
            {
                throw new EntityNotFoundException("TRIAL_NOT_FOUND");
            }

            logger.info("DB operation: SELECT trial_enrolments");
            Integer patientExists = namedParameterJdbcTemplate.query("SELECT 1 FROM trial_enrolments WHERE trial_id = :trialId AND patient_id = :patientId AND status = 'ENROLLED' LIMIT 1", new MapSqlParameterSource().addValue("trialId", coerced.trialId()).addValue("patientId", coerced.patientId()), rs -> rs.next() ? 1 : null);
            if (patientExists == null)
            {
                throw new EntityNotFoundException("PATIENT_NOT_FOUND");
            }

            logger.info("DB operation: SELECT adverse_events");
            MapSqlParameterSource duplicateParams = new MapSqlParameterSource()
                .addValue("trialId", coerced.trialId())
                .addValue("patientId", coerced.patientId())
                .addValue("aeTermCode", coerced.aeTermCode())
                .addValue("ctcaeGrade", coerced.ctcaeGrade())
                .addValue("windowStart", Timestamp.from(Instant.now().minusSeconds(60)));
            String duplicateAeId = namedParameterJdbcTemplate.query("SELECT ae_id FROM adverse_events WHERE trial_id = :trialId AND patient_id = :patientId AND ae_term_code = :aeTermCode AND ctcae_grade = :ctcaeGrade AND submitted_at >= :windowStart ORDER BY submitted_at DESC LIMIT 1", duplicateParams, rs -> rs.next() ? rs.getString("ae_id") : null);
            if (duplicateAeId != null)
            {
                throw new IllegalStateException("DUPLICATE_AE:" + duplicateAeId);
            }

            logger.info("DB operation: SELECT ae_id_seq");
            String aeId = namedParameterJdbcTemplate.queryForObject("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')", new MapSqlParameterSource(), String.class);
            logger.info("DB operation: SELECT notif_id_seq");
            String notificationId = namedParameterJdbcTemplate.queryForObject("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')", new MapSqlParameterSource(), String.class);

            Instant now = Instant.now();
            logger.info("DB operation: INSERT adverse_events");
            namedParameterJdbcTemplate.update("INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (:aeId, :trialId, :siteId, :patientId, :clinicianId, :eventDate, :aeTermCode, :aeTermName, :ctcaeGrade, :serious, :outcome, :actionTaken, :narrative, :relatedDrugId, :reportedBy, :submittedAt, :createdAt, :updatedAt)", new MapSqlParameterSource()
                .addValue("aeId", aeId)
                .addValue("trialId", coerced.trialId())
                .addValue("siteId", coerced.siteId())
                .addValue("patientId", coerced.patientId())
                .addValue("clinicianId", coerced.clinicianId())
                .addValue("eventDate", Timestamp.from(coerced.eventDate()))
                .addValue("aeTermCode", coerced.aeTermCode())
                .addValue("aeTermName", coerced.aeTermName())
                .addValue("ctcaeGrade", coerced.ctcaeGrade())
                .addValue("serious", coerced.serious())
                .addValue("outcome", coerced.outcome().name())
                .addValue("actionTaken", coerced.actionTaken().name())
                .addValue("narrative", coerced.narrative())
                .addValue("relatedDrugId", coerced.relatedDrugId())
                .addValue("reportedBy", coerced.reportedBy())
                .addValue("submittedAt", Timestamp.from(now))
                .addValue("createdAt", Timestamp.from(now))
                .addValue("updatedAt", Timestamp.from(now)));

            logger.info("DB operation: INSERT ae_notifications");
            AdverseEventPriority priority = coerced.ctcaeGrade() >= 3 ? AdverseEventPriority.HIGH : AdverseEventPriority.NORMAL;
            namedParameterJdbcTemplate.update("INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, acknowledged_by, acknowledged_at, sns_published, sns_message_id, created_at, updated_at) VALUES (:notificationId, :aeId, :trialId, :siteId, :patientId, :aeTermName, :ctcaeGrade, :serious, :outcome, :priority, false, null, null, false, null, :createdAt, :updatedAt)", new MapSqlParameterSource()
                .addValue("notificationId", notificationId)
                .addValue("aeId", aeId)
                .addValue("trialId", coerced.trialId())
                .addValue("siteId", coerced.siteId())
                .addValue("patientId", coerced.patientId())
                .addValue("aeTermName", coerced.aeTermName())
                .addValue("ctcaeGrade", coerced.ctcaeGrade())
                .addValue("serious", coerced.serious())
                .addValue("outcome", coerced.outcome().name())
                .addValue("priority", priority.name())
                .addValue("createdAt", Timestamp.from(now))
                .addValue("updatedAt", Timestamp.from(now)));

            try
            {
                logger.info("Notification stub executed");
            }
            catch (Exception ex)
            {
                logger.error("Notification stub error: {}", ex.getMessage());
            }

            return new AdverseEventResponseDto("success", aeId, notificationId, false, null, now.toString());
        }
        catch (DataAccessException ex)
        {
            logger.error("DB_ERROR: {}", ex.getMessage());
            throw new IllegalStateException("DB_ERROR");
        }
    }

    public AdverseEventNotificationResponseDto getNotifications(NotificationQueryDto query)
    {
        logger.info("Service entry: get notifications");
        List<String> conditions = new ArrayList<>();
        MapSqlParameterSource params = new MapSqlParameterSource();
        if (query.trialId() != null)
        {
            conditions.add("trial_id = :trialId");
            params.addValue("trialId", query.trialId());
        }
        if (query.siteId() != null)
        {
            conditions.add("site_id = :siteId");
            params.addValue("siteId", query.siteId());
        }
        if (query.ctcaeGrade() != null)
        {
            conditions.add("ctcae_grade = :ctcaeGrade");
            params.addValue("ctcaeGrade", query.ctcaeGrade());
        }
        if (query.serious() != null)
        {
            conditions.add("serious = :serious");
            params.addValue("serious", query.serious());
        }
        if (query.acknowledged() != null)
        {
            conditions.add("acknowledged = :acknowledged");
            params.addValue("acknowledged", query.acknowledged());
        }
        if (query.priority() != null)
        {
            conditions.add("priority = :priority");
            params.addValue("priority", query.priority().name());
        }
        if (query.dateFrom() != null)
        {
            conditions.add("created_at >= :dateFrom");
            params.addValue("dateFrom", Timestamp.from(query.dateFrom()));
        }
        if (query.dateTo() != null)
        {
            conditions.add("created_at <= :dateTo");
            params.addValue("dateTo", Timestamp.from(query.dateTo()));
        }
        String whereClause = conditions.isEmpty() ? "" : " WHERE " + String.join(" AND ", conditions);
        try
        {
            logger.info("DB operation: SELECT ae_notifications COUNT");
            Long total = namedParameterJdbcTemplate.queryForObject("SELECT COUNT(*) FROM ae_notifications" + whereClause, params, Long.class);
            int page = query.page() == null ? 1 : query.page();
            int pageSize = query.pageSize() == null ? 20 : Math.min(query.pageSize(), 100);
            params.addValue("limit", pageSize);
            params.addValue("offset", (page - 1) * pageSize);
            logger.info("DB operation: SELECT ae_notifications DATA");
            List<AdverseEventNotificationItemDto> notifications = namedParameterJdbcTemplate.query("SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, sns_published, created_at FROM ae_notifications" + whereClause + " ORDER BY created_at DESC LIMIT :limit OFFSET :offset", params, (rs, rowNum) -> new AdverseEventNotificationItemDto(rs.getString("notification_id"), rs.getString("ae_id"), rs.getString("trial_id"), rs.getString("site_id"), rs.getString("patient_id"), rs.getString("ae_term_name"), rs.getInt("ctcae_grade"), rs.getBoolean("serious"), AdverseEventPriority.valueOf(rs.getString("priority")), AdverseEventOutcome.valueOf(rs.getString("outcome")), rs.getBoolean("acknowledged"), rs.getBoolean("sns_published"), rs.getTimestamp("created_at").toInstant().toString()));
            return new AdverseEventNotificationResponseDto("success", total == null ? 0L : total, page, pageSize, notifications);
        }
        catch (DataAccessException ex)
        {
            logger.error("DB_ERROR: {}", ex.getMessage());
            throw new IllegalStateException("DB_ERROR");
        }
    }

    private AdverseEventRequestDto coerce(AdverseEventRequestDto request)
    {
        boolean serious = request.ctcaeGrade() >= 3 || request.serious();
        AdverseEventOutcome outcome = request.ctcaeGrade() == 5 ? AdverseEventOutcome.FATAL : request.outcome();
        return new AdverseEventRequestDto(request.trialId(), request.siteId(), request.patientId(), request.clinicianId(), request.eventDate(), request.aeTermCode(), request.aeTermName(), request.ctcaeGrade(), serious, outcome, request.actionTaken(), request.narrative(), request.relatedDrugId(), request.reportedBy());
    }

    private void validateBusinessRules(AdverseEventRequestDto request)
    {
        if (request.ctcaeGrade() < 1 || request.ctcaeGrade() > 5)
        {
            throw new IllegalArgumentException("INVALID_CTCAE_GRADE");
        }
        if (request.narrative() != null && request.narrative().length() > 2000)
        {
            throw new IllegalArgumentException("NARRATIVE_TOO_LONG");
        }
        if (request.ctcaeGrade() == 5 && request.outcome() != AdverseEventOutcome.FATAL)
        {
            return;
        }
    }
}
