package com.ai2dev.demo_travelcards_spring.controller;

import com.ai2dev.demo_travelcards_spring.dto.CreateTravelcardResponse;
import com.ai2dev.demo_travelcards_spring.dto.TravelcardRequest;
import com.ai2dev.demo_travelcards_spring.service.TravelcardService;
import java.time.OffsetDateTime;
import java.util.List;
import java.util.Map;
import java.util.regex.Pattern;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.validation.annotation.Validated;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import jakarta.validation.Valid;

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
    public ResponseEntity<?> createTravelcard(
            @RequestHeader(value = "client_id", required = true) String clientId,
            @RequestHeader HttpHeaders headers,
            @RequestHeader(value = "X-Correlation-Cust-Id", required = false) String correlationId,
            @Valid @RequestBody TravelcardRequest request) {
        log.info("createTravelcard - entry client_id={}", clientId);

        try {
            // Header validations
            if (clientId == null || clientId.length() < 1 || clientId.length() > 128) {
                log.error("client_id header missing or invalid length");
                return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(Map.of("error", "invalid client_id"));
            }

            Pattern clientPattern = Pattern.compile("^[A-Za-z0-9_+]+$");
            if (!clientPattern.matcher(clientId).matches()) {
                log.error("client_id header failed pattern");
                return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(Map.of("error", "invalid client_id format"));
            }

            // Content-Type check for requests with body
            String contentType = headers.getFirst(HttpHeaders.CONTENT_TYPE);
            if (contentType == null || !contentType.contains(MediaType.APPLICATION_JSON_VALUE)) {
                log.error("Content-Type missing or not application/json");
                return ResponseEntity.status(HttpStatus.UNSUPPORTED_MEDIA_TYPE).body(Map.of("error", "Content-Type must contain application/json"));
            }

            if (correlationId != null) {
                if (correlationId.length() > 100) {
                    log.error("X-Correlation-Cust-Id too long");
                    return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(Map.of("error", "invalid X-Correlation-Cust-Id"));
                }
                Pattern corr = Pattern.compile("^[A-Za-z0-9_-]+$");
                if (!corr.matcher(correlationId).matches()) {
                    log.error("X-Correlation-Cust-Id failed pattern");
                    return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(Map.of("error", "invalid X-Correlation-Cust-Id format"));
                }
            }

            // Business validations
            OffsetDateTime now = OffsetDateTime.now();

            if (!request.travelcardRequestedDate().isBefore(now)) {
                log.error("requested_date must be in the past");
                return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(Map.of("error", "travelcardRequestedDate must be in the past"));
            }

            if (request.travelcardValidFrom().isAfter(request.travelcardValidTo())) {
                log.error("travelcardValidFrom is after travelcardValidTo");
                return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(Map.of("error", "travelcardValidFrom must not be after travelcardValidTo"));
            }

            if (!request.travelcardValidTo().isAfter(now)) {
                log.error("travelcardValidTo must be in the future");
                return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(Map.of("error", "travelcardValidTo must be in the future"));
            }

            if (request.travelcardType() == null) {
                log.error("travelcardType is null");
                return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(Map.of("error", "travelcardType required"));
            }

            if (request.travelcardType().name().equals("SixteenToSeventeen")) {
                if (request.travelcardUsableTo() == null) {
                    log.error("usable_to required for SixteenToSeventeen");
                    return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(Map.of("error", "travelcardUsableTo is required for SixteenToSeventeen"));
                }
                if (!request.travelcardUsableTo().isAfter(now)) {
                    log.error("travelcardUsableTo must be in the future");
                    return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(Map.of("error", "travelcardUsableTo must be in the future"));
                }
            } else {
                if (request.travelcardUsableTo() != null) {
                    log.error("usable_to must be null for non SixteenToSeventeen types");
                    return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(Map.of("error", "travelcardUsableTo must not be provided unless type is SixteenToSeventeen"));
                }
            }

            List.of(request.cardholders()).size();
            if (request.cardholders() == null || request.cardholders().length < 1 || request.cardholders().length > 2) {
                log.error("cardholders count invalid");
                return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(Map.of("error", "cardholders must contain 1 or 2 items"));
            }

            int primaryCount = 0;
            int secondaryCount = 0;
            boolean secondaryPresent = false;
            for (var ch : request.cardholders()) {
                if (ch.cardholderType() == null) {
                    log.error("cardholderType missing");
                    return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(Map.of("error", "cardholderType required"));
                }
                if (ch.cardholderType().name().equals("Primary")) {
                    primaryCount++;
                } else if (ch.cardholderType().name().equals("Secondary")) {
                    secondaryCount++;
                    secondaryPresent = true;
                }

                if (ch.cardholderPhotoURL() == null && ch.cardholderPhotoKey() == null) {
                    log.error("cardholder must have either photo URL or photo key");
                    return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(Map.of("error", "each cardholder must provide cardholderPhotoURL or cardholderPhotoKey"));
                }

                if (ch.cardholderPhotoURL() != null) {
                    if (ch.cardholderPhotoURL().length() < 20 || ch.cardholderPhotoURL().length() > 2048) {
                        log.error("cardholderPhotoURL length invalid");
                        return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(Map.of("error", "cardholderPhotoURL length invalid"));
                    }
                }

                if (ch.cardholderPhotoKey() != null) {
                    if (ch.cardholderPhotoKey().length() < 39 || ch.cardholderPhotoKey().length() > 42) {
                        log.error("cardholderPhotoKey length invalid");
                        return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(Map.of("error", "cardholderPhotoKey length invalid"));
                    }
                }
            }

            if (primaryCount != 1) {
                log.error("must have exactly one Primary cardholder");
                return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(Map.of("error", "exactly one Primary cardholder required"));
            }

            if (secondaryCount > 1) {
                log.error("at most one Secondary allowed");
                return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(Map.of("error", "at most one Secondary cardholder allowed"));
            }

            // Secondary allowed only for certain travelcard types
            if (secondaryPresent) {
                switch (request.travelcardType()) {
                    case TwoTogether:
                    case Family:
                        break;
                    default:
                        log.error("Secondary cardholder not allowed for type={}", request.travelcardType());
                        return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(Map.of("error", "Secondary cardholder not allowed for this travelcard type"));
                }
            }

            CreateTravelcardResponse resp = service.createTravelcard(request);
            log.info("createTravelcard - exit travelcardId={}", resp.travelcardId());
            return ResponseEntity.status(HttpStatus.CREATED).body(Map.of("travelcardId", String.valueOf(resp.travelcardId()), "token", resp.token()));
        } catch (Exception e) {
            log.error("createTravelcard - error", e);
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body(Map.of("error", "internal_error"));
        }
    }
}
