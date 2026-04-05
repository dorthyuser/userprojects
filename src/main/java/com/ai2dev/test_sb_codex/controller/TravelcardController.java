package com.ai2dev.test_sb_codex.controller;

import com.ai2dev.test_sb_codex.dto.CreateTravelcardResponse;
import com.ai2dev.test_sb_codex.dto.TravelcardRequest;
import com.ai2dev.test_sb_codex.service.TravelcardService;
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
    public ResponseEntity<CreateTravelcardResponse> createTravelcard(
        @RequestHeader(name = "client_id") String clientId,
        @RequestHeader(name = "Content-Type") String contentType,
        @RequestHeader(name = "X-Correlation-Cust-Id", required = false) String correlationId,
        @Valid @RequestBody TravelcardRequest request
    ) {
        log.info("Entered createTravelcard");

        // Header validations
        if (clientId == null || clientId.isBlank() || clientId.length() > 128) {
            log.error("Invalid client_id header");
            return ResponseEntity.badRequest().build();
        }

        Pattern clientPattern = Pattern.compile("^[\\w+]+$");
        if (!clientPattern.matcher(clientId).matches()) {
            log.error("client_id pattern mismatch");
            return ResponseEntity.badRequest().build();
        }

        if (contentType == null || !contentType.equals(MediaType.APPLICATION_JSON_VALUE)) {
            log.error("Invalid Content-Type header");
            return ResponseEntity.status(415).build();
        }

        if (correlationId != null && (correlationId.length() > 100 || !Pattern.compile("^[A-Za-z0-9_-]+$").matcher(correlationId).matches())) {
            log.error("Invalid X-Correlation-Cust-Id header");
            return ResponseEntity.badRequest().build();
        }

        try {
            CreateTravelcardResponse resp = travelcardService.createTravelcard(request);
            log.info("Exiting createTravelcard");
            return ResponseEntity.ok(resp);
        } catch (IllegalArgumentException e) {
            log.error("Validation error creating travelcard", e);
            return ResponseEntity.badRequest().build();
        } catch (Exception e) {
            log.error("Internal error creating travelcard", e);
            return ResponseEntity.status(500).build();
        }
    }
}
