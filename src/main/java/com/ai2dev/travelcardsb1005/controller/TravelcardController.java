package com.ai2dev.travelcardsb1005.controller;

import com.ai2dev.travelcardsb1005.dto.TravelcardRequestDto;
import com.ai2dev.travelcardsb1005.dto.TravelcardResponseDto;
import com.ai2dev.travelcardsb1005.service.TravelcardService;
import jakarta.validation.Valid;
import java.util.Map;
import java.util.Optional;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
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
    private static final Logger log = LoggerFactory.getLogger(TravelcardController.class);

    private final TravelcardService travelcardService;

    public TravelcardController(TravelcardService travelcardService)
    {
        this.travelcardService = travelcardService;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<TravelcardResponseDto> createTravelcard(
        @RequestHeader Map<String, String> headers,
        @Valid @RequestBody TravelcardRequestDto request)
    {
        String clientId = extractHeader(headers, "client_id");
        String correlationId = extractHeader(headers, "X-Correlation-Cust-Id");
        log.info("controller=TravelcardController action=create method=POST path=/travelcards client_id={} correlation_id={}", clientId, correlationId);
        TravelcardResponseDto response = travelcardService.createTravelcard(clientId, correlationId, request);
        return ResponseEntity.status(HttpStatus.CREATED).body(response);
    }

    private String extractHeader(Map<String, String> headers, String name)
    {
        return Optional.ofNullable(headers.get(name.toLowerCase()))
            .orElse(headers.getOrDefault(name, headers.getOrDefault(name.toUpperCase(), headers.get(name.toLowerCase()))));
    }
}
