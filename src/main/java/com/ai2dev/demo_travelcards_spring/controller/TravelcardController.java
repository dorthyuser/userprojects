package com.ai2dev.demo_travelcards_spring.controller;

import com.ai2dev.demo_travelcards_spring.dto.TravelcardRequest;
import com.ai2dev.demo_travelcards_spring.dto.TravelcardResponse;
import com.ai2dev.demo_travelcards_spring.service.TravelcardService;
import jakarta.validation.Valid;
import java.util.regex.Pattern;
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
public class TravelcardController {
    private static final Logger log = LoggerFactory.getLogger(TravelcardController.class);
    private final TravelcardService travelcardService;

    public TravelcardController(TravelcardService travelcardService) {
        this.travelcardService = travelcardService;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<TravelcardResponse> createTravelcard(
        @RequestHeader("client_id") String clientId,
        @RequestHeader(value = "Content-Type", required = false) String contentType,
        @RequestHeader(value = "X-Correlation-Cust-Id", required = false) String correlationId,
        @Valid @RequestBody TravelcardRequest request
    ) {
        log.info("Enter createTravelcard clientId={}", clientId);

        if (clientId == null || clientId.isBlank()) {
            log.error("Missing client_id header");
            return ResponseEntity.badRequest().build();
        }

        if (!Pattern.matches("^[A-Za-z0-9_+]+$", clientId)) {
            log.error("Invalid client_id format");
            return ResponseEntity.badRequest().build();
        }

        if (contentType != null && !contentType.toLowerCase().contains("application/json")) {
            log.error("Invalid Content-Type header: {}", contentType);
            return ResponseEntity.badRequest().build();
        }

        if (correlationId != null && correlationId.length() > 100) {
            log.error("Invalid X-Correlation-Cust-Id length");
            return ResponseEntity.badRequest().build();
        }

        TravelcardResponse response = travelcardService.createTravelcard(request);
        log.info("Exit createTravelcard travelcardId={}", response.travelcardId());
        return ResponseEntity.ok(response);
    }
}
