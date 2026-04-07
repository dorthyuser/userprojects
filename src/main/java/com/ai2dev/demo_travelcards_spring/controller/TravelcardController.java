package com.ai2dev.demo_travelcards_spring.controller;

import com.ai2dev.demo_travelcards_spring.dto.CreateTravelcardRequest;
import com.ai2dev.demo_travelcards_spring.service.TravelcardService;
import java.util.HashMap;
import java.util.Map;
import jakarta.validation.Valid;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.validation.annotation.Validated;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/travelcards")
@Validated
public class TravelcardController {
    private static final Logger log = LoggerFactory.getLogger(TravelcardController.class);

    private final TravelcardService service;

    public TravelcardController(TravelcardService service) {
        this.service = service;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<Map<String, Object>> createTravelcard(
        @RequestHeader(value = "client_id", required = true) String clientId,
        @RequestHeader(value = "Content-Type", required = false) String contentType,
        @RequestHeader(value = "X-Correlation-Cust-Id", required = false) String correlationId,
        @Valid @RequestBody CreateTravelcardRequest request
    ) {
        log.info("Entry createTravelcard client_id={}", clientId);

        if (clientId == null || clientId.isBlank() || clientId.length() > 128 || !clientId.matches("^[A-Za-z0-9_+]+$")) {
            log.error("Invalid client_id header");
            return ResponseEntity.badRequest().body(Map.of("error", "Invalid client_id header"));
        }

        if (contentType == null || !contentType.toLowerCase().contains("application/json")) {
            log.error("Invalid Content-Type header");
            return ResponseEntity.badRequest().body(Map.of("error", "Content-Type must contain application/json"));
        }

        if (correlationId != null && (correlationId.length() > 100 || !correlationId.matches("^[A-Za-z0-9_-]+$"))) {
            log.error("Invalid X-Correlation-Cust-Id header");
            return ResponseEntity.badRequest().body(Map.of("error", "Invalid X-Correlation-Cust-Id header"));
        }

        Map<String, Object> result = service.create(request);
        Map<String, Object> response = new HashMap<>();
        response.put("travelcardId", result.get("travelcardId"));
        response.put("token", result.get("token"));

        log.info("Exit createTravelcard travelcardId={}", result.get("travelcardId"));
        return ResponseEntity.ok(response);
    }
}
