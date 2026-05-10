package com.ai2dev.travelcardspringboot501.controller;

import com.ai2dev.travelcardspringboot501.dto.TravelcardCreateRequestDto;
import com.ai2dev.travelcardspringboot501.dto.TravelcardCreateResponseDto;
import com.ai2dev.travelcardspringboot501.service.TravelcardService;
import jakarta.validation.Valid;
import java.util.Map;
import java.util.Optional;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
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
    private static final Logger logger = LoggerFactory.getLogger(TravelcardController.class);

    private final TravelcardService travelcardService;

    public TravelcardController(TravelcardService travelcardService)
    {
        this.travelcardService = travelcardService;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<TravelcardCreateResponseDto> createTravelcard(
            @RequestHeader Map<String, String> headers,
            @RequestHeader(value = "client_id", required = false) String clientId,
            @RequestHeader(value = "X-Correlation-Cust-Id", required = false) String correlationId,
            @Valid @RequestBody TravelcardCreateRequestDto request)
    {
        String resolvedClientId = Optional.ofNullable(headers.get("client_id")).orElse(headers.getOrDefault("Client_Id", headers.get("CLIENT_ID")));
        String resolvedCorrelationId = Optional.ofNullable(headers.get("x-correlation-cust-id")).orElse(headers.getOrDefault("X-Correlation-Cust-Id", headers.get("X-CORRELATION-CUST-ID")));
        logger.info("HTTP POST /travelcards client_id={} correlation_id={}", resolvedClientId, resolvedCorrelationId);
        return ResponseEntity.ok(travelcardService.createTravelcard(resolvedClientId, resolvedCorrelationId, request));
    }
}
