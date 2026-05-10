package com.ai2dev.testing330.controller;

import com.ai2dev.testing330.dto.CreateTravelcardRequestDto;
import com.ai2dev.testing330.dto.CreateTravelcardResponseDto;
import com.ai2dev.testing330.service.TravelcardService;
import jakarta.validation.Valid;
import java.util.Optional;
import java.util.Map;
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
            @Valid @RequestBody CreateTravelcardRequestDto request)
    {
        String clientId = Optional.ofNullable(headers.get("client_id"))
                .orElse(headers.getOrDefault("Client_Id", headers.get("CLIENT_ID")));
        String correlationId = Optional.ofNullable(headers.get("x-correlation-cust-id"))
                .orElse(headers.getOrDefault("X-Correlation-Cust-Id", headers.get("X-CORRELATION-CUST-ID")));
        logger.info("Controller entry method=POST path=/travelcards client_id={} x-correlation-cust-id={}", clientId, correlationId);
        CreateTravelcardResponseDto response = travelcardService.createTravelcard(request, clientId, correlationId);
        logger.info("Controller exit method=POST path=/travelcards");
        return ResponseEntity.status(HttpStatus.CREATED).body(response);
    }
}