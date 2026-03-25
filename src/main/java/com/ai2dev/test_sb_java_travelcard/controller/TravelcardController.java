package com.ai2dev.test_sb_java_travelcard.controller;

import com.ai2dev.test_sb_java_travelcard.dto.TravelcardRequest;
import com.ai2dev.test_sb_java_travelcard.dto.TravelcardResponse;
import com.ai2dev.test_sb_java_travelcard.service.TravelcardService;
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
import org.springframework.web.server.ResponseStatusException;
import org.springframework.http.HttpStatus;

@RestController
@RequestMapping("/travelcards")
public class TravelcardController {
    private static final Logger log = LoggerFactory.getLogger(TravelcardController.class);

    private final TravelcardService service;

    public TravelcardController(TravelcardService service) {
        this.service = service;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<TravelcardResponse> createTravelcard(
        @RequestHeader(value = "client_id") String clientId,
        @RequestHeader(value = "Content-Type", required = false) String contentType,
        @RequestHeader(value = "X-Correlation-Cust-Id", required = false) String correlationId,
        @Valid @RequestBody TravelcardRequest request
    ) {
        log.info("Entered createTravelcard");

        try {
            validateHeaders(clientId, contentType, correlationId);

            TravelcardResponse resp = service.createTravelcard(request);

            log.info("Exiting createTravelcard");
            return ResponseEntity.status(HttpStatus.CREATED).body(resp);
        } catch (ResponseStatusException ex) {
            log.error("Validation error in createTravelcard", ex);
            throw ex;
        } catch (Exception ex) {
            log.error("Unhandled error in createTravelcard", ex);
            throw new ResponseStatusException(HttpStatus.INTERNAL_SERVER_ERROR, "Internal server error");
        }
    }

    private void validateHeaders(String clientId, String contentType, String correlationId) {
        if (clientId == null || clientId.isBlank()) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Missing or empty client_id header");
        }

        if (clientId.length() < 1 || clientId.length() > 128) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "client_id length invalid");
        }

        if (!Pattern.matches("^[\\w+]+$", clientId)) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "client_id pattern invalid");
        }

      
        if (correlationId != null) {
            if (correlationId.length() > 100) {
                throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "X-Correlation-Cust-Id too long");
            }

            if (!Pattern.matches("^[A-Za-z0-9_-]+$", correlationId)) {
                throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "X-Correlation-Cust-Id pattern invalid");
            }
        }
    }
}
