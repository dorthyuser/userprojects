package com.ai2dev.demo_travelcard_sb.controller;

import com.ai2dev.demo_travelcard_sb.dto.CreateTravelcardRequest;
import com.ai2dev.demo_travelcard_sb.dto.CreateTravelcardResponse;
import com.ai2dev.demo_travelcard_sb.service.TravelcardService;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.validation.Valid;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.validation.BindingResult;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import java.util.HashMap;
import java.util.Map;
import java.util.regex.Pattern;

@RestController
@RequestMapping("/travelcards")
public class TravelcardController {
    private static final Logger log = LoggerFactory.getLogger(TravelcardController.class);

    private final TravelcardService travelcardService;

    public TravelcardController(TravelcardService travelcardService) {
        this.travelcardService = travelcardService;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> createTravelcard(
        HttpServletRequest servletRequest,
        @RequestHeader(value = "client_id", required = false) String clientId,
        @RequestHeader(value = "X-Correlation-Cust-Id", required = false) String correlationId,
        @Valid @RequestBody CreateTravelcardRequest request,
        BindingResult bindingResult
    ) {
        log.info("Enter createTravelcard");

        if (clientId == null || clientId.isBlank()) {
            log.error("Missing required header client_id");
            Map<String, String> err = new HashMap<>();
            err.put("error", "Missing required header client_id");
            return ResponseEntity.badRequest().body(err);
        }

        if (clientId.length() < 1 || clientId.length() > 128 || !Pattern.matches("^[A-Za-z0-9_+]+$", clientId)) {
            log.error("Invalid client_id header");
            Map<String, String> err = new HashMap<>();
            err.put("error", "Invalid client_id header");
            return ResponseEntity.badRequest().body(err);
        }

        String contentType = servletRequest.getContentType();
        if (contentType == null || !contentType.contains("application/json")) {
            log.error("Invalid or missing Content-Type header");
            Map<String, String> err = new HashMap<>();
            err.put("error", "Invalid or missing Content-Type header");
            return ResponseEntity.badRequest().body(err);
        }

        if (correlationId != null && (correlationId.length() > 100 || !Pattern.matches("^[A-Za-z0-9_-]+$", correlationId))) {
            log.error("Invalid X-Correlation-Cust-Id header");
            Map<String, String> err = new HashMap<>();
            err.put("error", "Invalid X-Correlation-Cust-Id header");
            return ResponseEntity.badRequest().body(err);
        }

        if (bindingResult.hasErrors()) {
            log.error("Validation errors in request body");
            Map<String, String> err = new HashMap<>();
            err.put("error", "Validation failed");
            return ResponseEntity.badRequest().body(err);
        }

        try {
            CreateTravelcardResponse response = travelcardService.createTravelcard(request);
            log.info("Exit createTravelcard");
            return ResponseEntity.ok(response);
        } catch (IllegalArgumentException ex) {
            log.error("Business validation failed: {}", ex.getMessage());
            Map<String, String> err = new HashMap<>();
            err.put("error", ex.getMessage());
            return ResponseEntity.badRequest().body(err);
        } catch (Exception ex) {
            log.error("Internal server error", ex);
            Map<String, String> err = new HashMap<>();
            err.put("error", "Internal server error");
            return ResponseEntity.status(500).body(err);
        }
    }
}
