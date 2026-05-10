package com.ai2dev.travelcardspringboot1248.controller;

import com.ai2dev.travelcardspringboot1248.dto.TravelcardCreateRequestDto;
import com.ai2dev.travelcardspringboot1248.dto.TravelcardCreateResponseDto;
import com.ai2dev.travelcardspringboot1248.service.TravelcardService;
import jakarta.validation.Valid;
import java.util.Map;
import java.util.Optional;
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
public class TravelcardController
{
    private static final Logger log = LoggerFactory.getLogger(TravelcardController.class);

    private final TravelcardService travelcardService;

    public TravelcardController(TravelcardService travelcardService)
    {
        this.travelcardService = travelcardService;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<TravelcardCreateResponseDto> createTravelcard(
            @RequestHeader Map<String, String> headers,
            @Valid @RequestBody TravelcardCreateRequestDto request)
    {
        String clientId = Optional.ofNullable(headers.get("client_id"))
                .orElse(headers.getOrDefault("Client_Id", headers.get("CLIENT_ID")));
        String correlationId = Optional.ofNullable(headers.get("x-correlation-cust-id"))
                .orElse(headers.getOrDefault("X-Correlation-Cust-Id", headers.get("X-CORRELATION-CUST-ID")));

        log.info("controller_entry method=POST path=/travelcards client_id_present={} correlation_id_present={}", clientId != null, correlationId != null);

        if (clientId == null || clientId.isBlank())
        {
            return ResponseEntity.status(HttpStatus.BAD_REQUEST).build();
        }

        TravelcardCreateResponseDto response = travelcardService.createTravelcard(request);
        log.info("controller_exit method=POST path=/travelcards status=201");
        return ResponseEntity.status(HttpStatus.CREATED).body(response);
    }
}