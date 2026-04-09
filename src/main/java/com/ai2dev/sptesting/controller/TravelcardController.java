package com.ai2dev.sptesting.controller;

import com.ai2dev.sptesting.dto.TravelcardRequest;
import com.ai2dev.sptesting.dto.TravelcardResponse;
import com.ai2dev.sptesting.service.TravelcardService;
import jakarta.validation.Valid;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import java.util.UUID;

@RestController
@RequestMapping("/api")
public class TravelcardController {
    private static final Logger logger = LoggerFactory.getLogger(TravelcardController.class);

    private final TravelcardService travelcardService;

    public TravelcardController(TravelcardService travelcardService) {
        this.travelcardService = travelcardService;
    }

    @PostMapping(path = "/travelcard", consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<TravelcardResponse> createTravelcard(
        @RequestHeader("client_id") String clientId,
        @RequestHeader(value = "X-Correlation-Cust-Id", required = false) String correlationId,
        @Valid @RequestBody TravelcardRequest request
    ) {
        logger.info("Enter createTravelcard client_id={} correlationId={}", clientId, correlationId);

        try {
            UUID travelcardId = travelcardService.createTravelcard(request);
            String token = travelcardService.generateToken();
            TravelcardResponse response = new TravelcardResponse(travelcardId.toString(), token);
            logger.info("Exit createTravelcard travelcardId={}", travelcardId);
            return ResponseEntity.status(201).body(response);
        } catch (IllegalArgumentException e) {
            logger.error("Validation error: {}", e.getMessage());
            return ResponseEntity.badRequest().body(new TravelcardResponse("", ""));
        } catch (Exception e) {
            logger.error("Internal error", e);
            return ResponseEntity.status(500).body(new TravelcardResponse("", ""));
        }
    }
}
