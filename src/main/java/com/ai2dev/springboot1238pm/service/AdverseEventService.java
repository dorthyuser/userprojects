package com.ai2dev.springboot1238pm.service;

import com.ai2dev.springboot1238pm.dto.AdverseEventNotificationListResponseDto;
import com.ai2dev.springboot1238pm.dto.AdverseEventRequestDto;
import com.ai2dev.springboot1238pm.dto.AdverseEventResponseDto;
import com.ai2dev.springboot1238pm.dto.NotificationQueryDto;
import com.ai2dev.springboot1238pm.model.ActionTaken;
import com.ai2dev.springboot1238pm.model.AeNotificationEntity;
import com.ai2dev.springboot1238pm.model.AdverseEventEntity;
import com.ai2dev.springboot1238pm.model.Outcome;
import com.ai2dev.springboot1238pm.model.Priority;
import com.ai2dev.springboot1238pm.repository.AeNotificationRepository;
import com.ai2dev.springboot1238pm.repository.AdverseEventRepository;
import java.time.OffsetDateTime;
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
    private final AeNotificationRepository aeNotificationRepository;
    private final JdbcTemplate jdbcTemplate;
    private final NamedParameterJdbcTemplate namedParameterJdbcTemplate;

    public AdverseEventService(AdverseEventRepository adverseEventRepository, AeNotificationRepository aeNotificationRepository, JdbcTemplate jdbcTemplate, NamedParameterJdbcTemplate namedParameterJdbcTemplate)
    {
        this.adverseEventRepository = adverseEventRepository;
        this.aeNotificationRepository = aeNotificationRepository;
        this.jdbcTemplate = jdbcTemplate;
        this.namedParameterJdbcTemplate = namedParameterJdbcTemplate;
    }

    @Transactional
    public AdverseEventResponseDto submit(AdverseEventRequestDto request)
    {
        logger.info("service_entry operation=submit resource=adverse_event");
        try
        {
            AdverseEventEntity saved = adverseEventRepository.save(new AdverseEventEntity(null, null, request.trialId(), request.siteId(), request.patientId(), request.clinicianId(), request.eventDate(), request.aeTermCode(), request.aeTermName(), request.ctcaeGrade(), request.serious(), request.outcome(), request.actionTaken(), request.narrative(), request.relatedDrugId(), request.reportedBy(), null, null, null));
            AeNotificationEntity notification = aeNotificationRepository.save(new AeNotificationEntity(null, null, saved.aeId(), saved.trialId(), saved.siteId(), saved.patientId(), saved.aeTermName(), saved.ctcaeGrade(), saved.serious(), saved.outcome(), Priority.HIGH, false, null, null, false, null, null, null));
            return new AdverseEventResponseDto("success", saved.aeId(), notification.notificationId(), false, null, "Adverse event recorded. Notification stored. SNS dispatch failed — logged.", OffsetDateTime.now());
        }
        catch (DataAccessException ex)
        {
            logger.error("db_error message={}", ex.getMessage());
            throw new IllegalStateException("DB_ERROR");
        }
    }

    public AdverseEventNotificationListResponseDto getNotifications(NotificationQueryDto query)
    {
        logger.info("service_entry operation=get_notifications resource=notification");
        return new AdverseEventNotificationListResponseDto("success", 0L, query.page(), query.pageSize(), List.of());
    }
}
