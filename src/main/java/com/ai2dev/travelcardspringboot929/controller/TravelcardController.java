package com.ai2dev.travelcardspringboot929.controller;

import com.ai2dev.travelcardspringboot929.dto.TravelcardRequestDto;
import com.ai2dev.travelcardspringboot929.dto.TravelcardResponseDto;
import com.ai2dev.travelcardspringboot929.service.TravelcardService;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.validation.Valid;
import java.util.Optional;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RestController;

@RestController
public class TravelcardController {

    private static final Logger logger = LoggerFactory.getLogger(TravelcardController.class);

    private final TravelcardService travelcardService;

    public TravelcardController(TravelcardService travelcardService) {
        this.travelcardService = travelcardService;
    }

    @PostMapping(value = "/travelcards", consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<TravelcardResponseDto> createTravelcard(
            @RequestHeader HttpHeaders headers,
            @RequestBody @Valid TravelcardRequestDto request,
            HttpServletRequest servletRequest) {
        String clientId = headerValue(headers, "client_id");
        String correlationId = headerValue(headers, "X-Correlation-Cust-Id");
        logger.info("entry method={} path={} client_id={} correlation_id={}", servletRequest.getMethod(), servletRequest.getRequestURI(), clientId, correlationId);
        TravelcardResponseDto response = travelcardService.createTravelcard(request, clientId, correlationId);
        logger.info("exit method={} path={} client_id={} correlation_id={}", servletRequest.getMethod(), servletRequest.getRequestURI(), clientId, correlationId);
        return ResponseEntity.ok(response);
    }

    private String headerValue(HttpHeaders headers, String name) {
        return Optional.ofNullable(headers.getFirst(name))
                .orElse(headers.getFirst(name.toLowerCase()));
    }
}
