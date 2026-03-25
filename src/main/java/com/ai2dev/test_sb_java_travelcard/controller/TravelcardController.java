package com.ai2dev.test_sb_java_travelcard.controller;

import com.ai2dev.test_sb_java_travelcard.dto.TravelcardRequest;
import com.ai2dev.test_sb_java_travelcard.dto.TravelcardResponse;
import com.ai2dev.test_sb_java_travelcard.service.TravelcardService;
import jakarta.validation.Valid;
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
public class TravelcardController {
    private static final Logger logger = LoggerFactory.getLogger(TravelcardController.class);

    private final TravelcardService travelcardService;

    public TravelcardController(TravelcardService travelcardService) {
        this.travelcardService = travelcardService;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> createTravelcard(
        @RequestHeader(value = "client_id") String clientId,
        @RequestHeader(value = "Content-Type") String contentType,
        @RequestHeader(value = "X-Correlation-Cust-Id", required = false) String correlationId,
        @Valid @RequestBody TravelcardRequest request
    ) {
        logger.info("Enter createTravelcard clientId={}", clientId);

        try {
            if (clientId == null || clientId.isBlank() || clientId.length() > 128 || !clientId.matches("[\\w+]+")) {
                logger.error("Invalid client_id header");
                return ResponseEntity.status(HttpStatus.BAD_REQUEST).body("Invalid client_id header");
            }

            if (contentType == null || !contentType.contains(MediaType.APPLICATION_JSON_VALUE)) {
                logger.error("Content-Type must be application/json");
                return ResponseEntity.status(HttpStatus.BAD_REQUEST).body("Content-Type must be application/json");
            }

            if (correlationId != null && (correlationId.length() > 100 || !correlationId.matches("^[A-Za-z0-9_-]+$"))) {
                logger.error("Invalid X-Correlation-Cust-Id header");
                return ResponseEntity.status(HttpStatus.BAD_REQUEST).body("Invalid X-Correlation-Cust-Id header");
            }

            TravelcardResponse resp = travelcardService.createTravelcard(request);
            logger.info("Exit createTravelcard travelcardId={}", resp.travelcardId());
            return ResponseEntity.status(HttpStatus.CREATED).body(resp);
        } catch (IllegalArgumentException e) {
            logger.error("Validation error", e);
            return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(e.getMessage());
        } catch (Exception e) {
            logger.error("Internal error", e);
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body("Internal error");
        }
    }
}
