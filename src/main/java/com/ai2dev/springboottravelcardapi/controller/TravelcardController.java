package com.ai2dev.springboottravelcardapi.controller;

import com.ai2dev.springboottravelcardapi.model.dto.TravelcardCreateRequestDto;
import com.ai2dev.springboottravelcardapi.model.dto.TravelcardCreateResponseDto;
import com.ai2dev.springboottravelcardapi.service.TravelcardService;
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
            @RequestHeader HttpHeaders headers,
            @Valid @RequestBody TravelcardCreateRequestDto request)
    {
        String clientId = headerValue(headers, "client_id");
        String correlationId = headerValue(headers, "X-Correlation-Cust-Id");
        logger.info("HTTP POST /travelcards client_id={} correlation_id_present={}", mask(clientId), correlationId != null);
        TravelcardCreateResponseDto response = travelcardService.createTravelcard(request, clientId, correlationId);
        logger.info("Completed HTTP POST /travelcards travelcard_id={}", response.travelcardId());
        return ResponseEntity.ok(response);
    }

    private String headerValue(HttpHeaders headers, String name)
    {
        return Optional.ofNullable(headers.getFirst(name))
                .orElse(headers.getFirst(name.toLowerCase()));
    }

    private String mask(String value)
    {
        if (value == null || value.isBlank())
        {
            return "null";
        }
        return "***";
    }
}
