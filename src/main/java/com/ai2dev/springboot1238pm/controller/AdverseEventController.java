package com.ai2dev.springboot1238pm.controller;

import com.ai2dev.springboot1238pm.dto.AdverseEventNotificationListResponseDto;
import com.ai2dev.springboot1238pm.dto.AdverseEventRequestDto;
import com.ai2dev.springboot1238pm.dto.AdverseEventResponseDto;
import com.ai2dev.springboot1238pm.dto.NotificationQueryDto;
import com.ai2dev.springboot1238pm.service.AdverseEventService;
import jakarta.validation.Valid;
import java.util.Map;
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
    public ResponseEntity<AdverseEventResponseDto> submit(@Valid @RequestBody AdverseEventRequestDto request)
    {
        logger.info("controller_entry method=POST path=/v1/adverse-events");
        return ResponseEntity.status(HttpStatus.CREATED).body(adverseEventService.submit(request));
    }

    @GetMapping("/notifications")
    public ResponseEntity<AdverseEventNotificationListResponseDto> getNotifications(@RequestParam Map<String, String> params)
    {
        logger.info("controller_entry method=GET path=/v1/adverse-events/notifications");
        return ResponseEntity.ok(adverseEventService.getNotifications(NotificationQueryDto.from(params)));
    }
}
