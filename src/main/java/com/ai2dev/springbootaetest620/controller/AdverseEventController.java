package com.ai2dev.springbootaetest620.controller;

import com.ai2dev.springbootaetest620.model.dto.AdverseEventNotificationResponseDto;
import com.ai2dev.springbootaetest620.model.dto.AdverseEventRequestDto;
import com.ai2dev.springbootaetest620.model.dto.AdverseEventResponseDto;
import com.ai2dev.springbootaetest620.model.dto.NotificationQueryDto;
import com.ai2dev.springbootaetest620.service.AdverseEventService;
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
    public ResponseEntity<AdverseEventResponseDto> submit(@Valid @RequestBody AdverseEventRequestDto request)
    {
        logger.info("HTTP POST /v1/adverse-events");
        AdverseEventResponseDto response = adverseEventService.submit(request);
        return ResponseEntity.status(HttpStatus.CREATED).body(response);
    }

    @GetMapping("/notifications")
    public ResponseEntity<AdverseEventNotificationResponseDto> notifications(@Valid NotificationQueryDto query)
    {
        logger.info("HTTP GET /v1/adverse-events/notifications");
        AdverseEventNotificationResponseDto response = adverseEventService.getNotifications(query);
        return ResponseEntity.ok(response);
    }
}
