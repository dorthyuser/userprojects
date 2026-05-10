package com.ai2dev.travelcardsb949.controller;

import com.ai2dev.travelcardsb949.dto.CreateTravelcardRequestDto;
import com.ai2dev.travelcardsb949.dto.CreateTravelcardResponseDto;
import com.ai2dev.travelcardsb949.service.TravelcardService;
import jakarta.validation.Valid;
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
    public ResponseEntity<CreateTravelcardResponseDto> createTravelcard(@RequestHeader Map<String, String> headers, @Valid @RequestBody CreateTravelcardRequestDto request)
    {
        String clientId = header(headers, "client_id");
        String correlationId = header(headers, "X-Correlation-Cust-Id");
        logger.info("Entering createTravelcard method=POST path=/travelcards client_id={} correlation_id={}", clientId, correlationId);
        if (clientId == null || clientId.isBlank())
        {
            logger.warn("Missing required header field=client_id reason=must be provided");
            return ResponseEntity.status(HttpStatus.BAD_REQUEST).build();
        }
        CreateTravelcardResponseDto response = travelcardService.createTravelcard(request);
        logger.info("Exiting createTravelcard method=POST path=/travelcards client_id={} correlation_id={}", clientId, correlationId);
        return ResponseEntity.status(HttpStatus.CREATED).body(response);
    }

    private String header(Map<String, String> headers, String name)
    {
        return java.util.Optional.ofNullable(headers.get(name.toLowerCase()))
            .orElse(headers.getOrDefault(name, headers.get(name.toUpperCase())));
    }
}