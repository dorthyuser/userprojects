package com.ai2dev.travelcardspdemo334.controller;

import com.ai2dev.travelcardspdemo334.dto.CreateTravelcardRequestDto;
import com.ai2dev.travelcardspdemo334.dto.CreateTravelcardResponseDto;
import com.ai2dev.travelcardspdemo334.service.TravelcardService;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.validation.Valid;
import java.util.HashMap;
import java.util.Map;
import java.util.Optional;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/travelcards")
public class TravelcardController
{
    private static final Logger logger = LoggerFactory.getLogger(TravelcardController.class);

    private final TravelcardService travelcardService;

    public TravelcardController(TravelcardService travelcardService)
    {
        this.travelcardService = travelcardService;
    }

    @PostMapping
    public ResponseEntity<CreateTravelcardResponseDto> createTravelcard(
            @RequestHeader Map<String, String> headers,
            @RequestBody @Valid CreateTravelcardRequestDto request,
            HttpServletRequest httpServletRequest)
    {
        String clientId = Optional.ofNullable(headers.get("client_id"))
                .orElse(headers.getOrDefault("Client-Id", headers.get("CLIENT_ID")));
        String correlationId = Optional.ofNullable(headers.get("x-correlation-cust-id"))
                .orElse(headers.getOrDefault("X-Correlation-Cust-Id", headers.get("X-CORRELATION-CUST-ID")));
        logger.info("Controller entry method=POST path={} client_id={} correlation_id={}", httpServletRequest.getRequestURI(), clientId, correlationId);
        CreateTravelcardResponseDto response = travelcardService.createTravelcard(request, clientId, correlationId);
        logger.info("Controller exit method=POST path={} client_id={} correlation_id={}", httpServletRequest.getRequestURI(), clientId, correlationId);
        return ResponseEntity.status(HttpStatus.CREATED).body(response);
    }
}
