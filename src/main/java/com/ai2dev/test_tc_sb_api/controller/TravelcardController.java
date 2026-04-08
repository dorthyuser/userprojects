package com.ai2dev.test_tc_sb_api.controller;

import com.ai2dev.test_tc_sb_api.dto.TravelcardRequest;
import com.ai2dev.test_tc_sb_api.dto.TravelcardResponse;
import com.ai2dev.test_tc_sb_api.model.Cardholder;
import com.ai2dev.test_tc_sb_api.model.Travelcard;
import com.ai2dev.test_tc_sb_api.service.TravelcardService;

import jakarta.servlet.http.HttpServletRequest;
import jakarta.validation.Valid;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.validation.annotation.Validated;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.server.ResponseStatusException;

import java.util.HashMap;
import java.util.List;
import java.util.Map;

@Validated
@RestController
@RequestMapping("/travelcards")
public class TravelcardController {
    private static final Logger logger = LoggerFactory.getLogger(TravelcardController.class);

    private final TravelcardService travelcardService;

    public TravelcardController(TravelcardService travelcardService) {
        this.travelcardService = travelcardService;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<TravelcardResponse> create(
        @RequestHeader("client_id") @Size(min = 1, max = 128) @Pattern(regexp = "^[A-Za-z0-9_+]+$") String clientId,
        @RequestHeader(value = "X-Correlation-Cust-Id", required = false) @Size(max = 100) @Pattern(regexp = "^[A-Za-z0-9_-]+$") String correlationId,
        @Valid @RequestBody TravelcardRequest request,
        HttpServletRequest httpRequest
    ) {
        logger.info("POST /travelcards called client_id={} correlationId={}", clientId, correlationId);

        String contentType = httpRequest.getHeader("Content-Type");
        if (contentType == null || !contentType.contains("application/json")) {
            logger.error("Invalid Content-Type: {}", contentType);
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Content-Type must contain application/json");
        }

        try {
            TravelcardResponse resp = travelcardService.create(request);
            logger.info("Travelcard created id={}", resp.getTravelcardId());
            return ResponseEntity.status(HttpStatus.CREATED).body(resp);
        } catch (IllegalArgumentException e) {
            logger.error("Validation error: {}", e.getMessage());
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, e.getMessage(), e);
        } catch (Exception e) {
            logger.error("Internal error", e);
            throw new ResponseStatusException(HttpStatus.INTERNAL_SERVER_ERROR, "Internal error", e);
        }
    }

    @GetMapping(value = "/{id}", produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<Map<String, Object>> getById(@PathVariable("id") Integer id) {
        logger.info("GET /travelcards/{}", id);
        Travelcard t = travelcardService.getById(id);
        if (t == null) {
            logger.error("Travelcard not found id={}", id);
            return ResponseEntity.status(HttpStatus.NOT_FOUND).body(null);
        }
        List<Cardholder> holders = travelcardService.getCardholdersFor(id);
        Map<String, Object> resp = new HashMap<>();
        resp.put("travelcard", t);
        resp.put("cardholders", holders);
        logger.info("Returning travelcard id={} with {} cardholders", id, holders == null ? 0 : holders.size());
        return ResponseEntity.ok(resp);
    }
}
