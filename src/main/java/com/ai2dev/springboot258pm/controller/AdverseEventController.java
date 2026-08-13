package com.ai2dev.springboot258pm.controller;

import com.ai2dev.springboot258pm.dto.AdverseEventNotificationResponseDto;
import com.ai2dev.springboot258pm.dto.AdverseEventRequestDto;
import com.ai2dev.springboot258pm.dto.AdverseEventResponseDto;
import com.ai2dev.springboot258pm.dto.NotificationSearchResponseDto;
import com.ai2dev.springboot258pm.service.AdverseEventService;
import jakarta.validation.Valid;
import java.util.Optional;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/v1/adverse-events")
public class AdverseEventController
{
    private static final Logger logger = LoggerFactory.getLogger(AdverseEventController.class);

    private final AdverseEventService adverseEventService;

    public AdverseEventController(AdverseEventService adverseEventService)
    {
        this.adverseEventService = adverseEventService;
    }

    @PostMapping
    public ResponseEntity<AdverseEventResponseDto> submitAdverseEvent(@Valid @RequestBody AdverseEventRequestDto request)
    {
        logger.info("HTTP POST /v1/adverse-events");
        return ResponseEntity.status(HttpStatus.CREATED).body(adverseEventService.submitAdverseEvent(request));
    }

    @GetMapping("/notifications")
    public ResponseEntity<NotificationSearchResponseDto> getNotifications(
            @RequestParam(required = false) String trialId,
            @RequestParam(required = false) String siteId,
            @RequestParam(required = false) Integer ctcaeGrade,
            @RequestParam(required = false) Boolean serious,
            @RequestParam(required = false) Boolean acknowledged,
            @RequestParam(required = false) String priority,
            @RequestParam(required = false) String dateFrom,
            @RequestParam(required = false) String dateTo,
            @RequestParam(required = false) Integer page,
            @RequestParam(required = false) Integer pageSize)
    {
        logger.info("HTTP GET /v1/adverse-events/notifications");
        return ResponseEntity.ok(adverseEventService.getNotifications(trialId, siteId, ctcaeGrade, serious, acknowledged, priority, dateFrom, dateTo, Optional.ofNullable(page).orElse(1), Optional.ofNullable(pageSize).orElse(20)));
    }
}