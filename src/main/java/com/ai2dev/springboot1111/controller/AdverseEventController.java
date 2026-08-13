package com.ai2dev.springboot1111.controller;

import com.ai2dev.springboot1111.dto.AdverseEventNotificationResponseDto;
import com.ai2dev.springboot1111.dto.AdverseEventRequestDto;
import com.ai2dev.springboot1111.dto.AdverseEventResponseDto;
import com.ai2dev.springboot1111.dto.NotificationSearchResponseDto;
import com.ai2dev.springboot1111.service.AdverseEventService;
import jakarta.validation.Valid;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import java.time.Instant;
import java.util.Optional;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.format.annotation.DateTimeFormat;
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
        AdverseEventResponseDto response = adverseEventService.submitAdverseEvent(request);
        return ResponseEntity.status(HttpStatus.CREATED).body(response);
    }

    @GetMapping("/notifications")
    public ResponseEntity<NotificationSearchResponseDto> getNotifications(
            @RequestParam(required = false) String trialId,
            @RequestParam(required = false) String siteId,
            @RequestParam(required = false) Integer ctcaeGrade,
            @RequestParam(required = false) Boolean serious,
            @RequestParam(required = false) Boolean acknowledged,
            @RequestParam(required = false) String priority,
            @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant dateFrom,
            @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant dateTo,
            @RequestParam(required = false, defaultValue = "1") @Min(1) Integer page,
            @RequestParam(required = false, defaultValue = "20") @Min(1) @Max(100) Integer pageSize)
    {
        logger.info("HTTP GET /v1/adverse-events/notifications");
        NotificationSearchResponseDto response = adverseEventService.getNotifications(trialId, siteId, ctcaeGrade, serious, acknowledged, priority, dateFrom, dateTo, page, pageSize);
        return ResponseEntity.ok(response);
    }
}
