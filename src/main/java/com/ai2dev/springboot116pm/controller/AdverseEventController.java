package com.ai2dev.springboot116pm.controller;

import com.ai2dev.springboot116pm.dto.AdverseEventNotificationPageResponseDto;
import com.ai2dev.springboot116pm.dto.AdverseEventSubmissionRequestDto;
import com.ai2dev.springboot116pm.dto.AdverseEventSubmissionResponseDto;
import com.ai2dev.springboot116pm.service.AdverseEventService;
import jakarta.validation.Valid;
import java.time.Instant;
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
    public ResponseEntity<AdverseEventSubmissionResponseDto> submit(@Valid @RequestBody AdverseEventSubmissionRequestDto request)
    {
        logger.info("HTTP POST /v1/adverse-events");
        AdverseEventSubmissionResponseDto response = adverseEventService.submit(request);
        return ResponseEntity.status(HttpStatus.CREATED).body(response);
    }

    @GetMapping("/notifications")
    public ResponseEntity<AdverseEventNotificationPageResponseDto> getNotifications(
            @RequestParam(required = false) String trialId,
            @RequestParam(required = false) String siteId,
            @RequestParam(required = false) Integer ctcaeGrade,
            @RequestParam(required = false) Boolean serious,
            @RequestParam(required = false) Boolean acknowledged,
            @RequestParam(required = false) String priority,
            @RequestParam(required = false) Instant dateFrom,
            @RequestParam(required = false) Instant dateTo,
            @RequestParam(required = false, defaultValue = "1") Integer page,
            @RequestParam(required = false, defaultValue = "20") Integer pageSize)
    {
        logger.info("HTTP GET /v1/adverse-events/notifications");
        return ResponseEntity.ok(adverseEventService.getNotifications(trialId, siteId, ctcaeGrade, serious, acknowledged, priority, dateFrom, dateTo, page, pageSize));
    }
}