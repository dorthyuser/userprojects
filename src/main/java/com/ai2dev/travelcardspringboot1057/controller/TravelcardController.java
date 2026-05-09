package com.ai2dev.travelcardspringboot1057.controller;

import com.ai2dev.travelcardspringboot1057.dto.CreateTravelcardResponseDto;
import com.ai2dev.travelcardspringboot1057.dto.TravelcardRequestDto;
import com.ai2dev.travelcardspringboot1057.service.TravelcardService;
import jakarta.validation.Valid;
import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/travelcards")
public class TravelcardController {

    private static final Logger log = LoggerFactory.getLogger(TravelcardController.class);
    private final TravelcardService travelcardService;

    public TravelcardController(TravelcardService travelcardService) {
        this.travelcardService = travelcardService;
    }

    @PostMapping
    public ResponseEntity<CreateTravelcardResponseDto> createTravelcard(
            @RequestHeader Map<String, String> headers,
            @Valid @RequestBody TravelcardRequestDto request) {
        log.info("Entering POST /travelcards client_id={} correlation_id={}", header(headers, "client_id"), header(headers, "X-Correlation-Cust-Id"));
        CreateTravelcardResponseDto response = travelcardService.createTravelcard(request);
        log.info("Exiting POST /travelcards travelcardId={}", response.travelcardId());
        return ResponseEntity.ok(response);
    }

    private String header(Map<String, String> headers, String name) {
        return java.util.Optional.ofNullable(headers.get(name.toLowerCase()))
                .orElse(headers.getOrDefault(name, headers.get(name.toUpperCase())));
    }
}
