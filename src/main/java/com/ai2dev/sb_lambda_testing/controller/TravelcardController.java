package com.ai2dev.sb_lambda_testing.controller;

import com.ai2dev.sb_lambda_testing.dto.TravelcardRequest;
import com.ai2dev.sb_lambda_testing.dto.TravelcardResponse;
import com.ai2dev.sb_lambda_testing.service.TravelcardService;
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

@RestController
@RequestMapping("/travelcards")
public class TravelcardController {
    private static final Logger logger = LoggerFactory.getLogger(TravelcardController.class);

    private final TravelcardService travelcardService;

    public TravelcardController(TravelcardService travelcardService) {
        this.travelcardService = travelcardService;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<TravelcardResponse> createTravelcard(
        @RequestHeader(name = "client_id") String clientId,
        @RequestHeader(name = "X-Correlation-Cust-Id", required = false) String correlationId,
        @Valid @RequestBody TravelcardRequest request
    ) {
        logger.info("Enter createTravelcard client_id={} correlationId={}", clientId, correlationId);

        if (clientId == null || clientId.isBlank() || clientId.length() > 128) {
            logger.error("Invalid client_id header");
            return ResponseEntity.badRequest().build();
        }

        try {
            TravelcardResponse response = travelcardService.createTravelcard(request, clientId, correlationId);

            logger.info("Exit createTravelcard travelcardId={}", response.travelcardId());

            return ResponseEntity.ok(response);
        } catch (IllegalArgumentException e) {
            logger.error("Validation error: {}", e.getMessage(), e);
            return ResponseEntity.badRequest().body(new TravelcardResponse("", ""));
        } catch (Exception e) {
            logger.error("Unexpected error creating travelcard", e);
            return ResponseEntity.status(500).body(new TravelcardResponse("", ""));
        }
    }
}
