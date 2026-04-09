package com.ai2dev.sb_lambda_testng_2.controller;

import com.ai2dev.sb_lambda_testng_2.dto.TravelcardRequest;
import com.ai2dev.sb_lambda_testng_2.dto.TravelcardResponse;
import com.ai2dev.sb_lambda_testng_2.service.TravelcardService;
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
    private static final Logger log = LoggerFactory.getLogger(TravelcardController.class);

    private final TravelcardService travelcardService;

    public TravelcardController(TravelcardService travelcardService) {
        this.travelcardService = travelcardService;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<TravelcardResponse> createTravelcard(
            @RequestHeader(value = "client_id") String clientId,
            @RequestHeader(value = "X-Correlation-Cust-Id", required = false) String correlationId,
            @Valid @RequestBody TravelcardRequest request) {
        log.info("Enter createTravelcard clientId={} correlationId={}", clientId, correlationId);

        if (clientId == null || clientId.isBlank() || clientId.length() > 128) {
            log.error("Invalid client_id header");
            return ResponseEntity.status(HttpStatus.BAD_REQUEST).build();
        }

        try {
            TravelcardResponse resp = travelcardService.createTravelcard(request, clientId, correlationId);
            log.info("Exit createTravelcard generated travelcardId={}", resp.travelcardId());
            return ResponseEntity.status(HttpStatus.CREATED).headers(new HttpHeaders()).body(resp);
        } catch (IllegalArgumentException e) {
            log.error("Validation error: {}", e.getMessage());
            return ResponseEntity.status(HttpStatus.BAD_REQUEST).build();
        } catch (Exception e) {
            log.error("Internal error: {}", e.getMessage(), e);
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).build();
        }
    }
}
