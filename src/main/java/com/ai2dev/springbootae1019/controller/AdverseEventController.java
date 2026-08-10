package com.ai2dev.springbootae1019.controller;

import com.ai2dev.springbootae1019.dto.AdverseEventNotificationPageResponseDto;
import com.ai2dev.springbootae1019.dto.AdverseEventRequestDto;
import com.ai2dev.springbootae1019.dto.AdverseEventResponseDto;
import com.ai2dev.springbootae1019.dto.NotificationFilterRequestDto;
import com.ai2dev.springbootae1019.service.AdverseEventService;
import jakarta.validation.Valid;
import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
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
    public ResponseEntity<AdverseEventResponseDto> submitAdverseEvent(@Valid @RequestBody AdverseEventRequestDto requestDto, @RequestHeader Map<String, String> headers)
    {
        logger.info("HTTP POST /v1/adverse-events");
        AdverseEventResponseDto responseDto = adverseEventService.submitAdverseEvent(requestDto);
        return ResponseEntity.status(HttpStatus.CREATED).body(responseDto);
    }

    @GetMapping("/notifications")
    public ResponseEntity<AdverseEventNotificationPageResponseDto> getNotifications(
            @RequestParam(required = false) String trialId,
            @RequestParam(required = false) String siteId,
            @RequestParam(required = false) Integer ctcaeGrade,
            @RequestParam(required = false) Boolean serious,
            @RequestParam(required = false) Boolean acknowledged,
            @RequestParam(required = false) String priority,
            @RequestParam(required = false) String dateFrom,
            @RequestParam(required = false) String dateTo,
            @RequestParam(required = false, defaultValue = "1") Integer page,
            @RequestParam(required = false, defaultValue = "20") Integer pageSize)
    {
        logger.info("HTTP GET /v1/adverse-events/notifications");
        NotificationFilterRequestDto filter = new NotificationFilterRequestDto(trialId, siteId, ctcaeGrade, serious, acknowledged, priority, dateFrom, dateTo, page, pageSize);
        return ResponseEntity.ok(adverseEventService.getNotifications(filter));
    }
}
