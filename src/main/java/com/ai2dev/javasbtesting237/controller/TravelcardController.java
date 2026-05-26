package com.ai2dev.javasbtesting237.controller;

import com.ai2dev.javasbtesting237.model.dto.CreateTravelcardRequestDto;
import com.ai2dev.javasbtesting237.model.dto.CreateTravelcardResponseDto;
import com.ai2dev.javasbtesting237.service.TravelcardService;
import jakarta.validation.Valid;
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

    private final TravelcardService travelcardService;

    public TravelcardController(TravelcardService travelcardService) {
        this.travelcardService = travelcardService;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<CreateTravelcardResponseDto> createTravelcard(
            @RequestHeader(name = "client_id") String clientId,
            @RequestHeader(name = "Content-Type") String contentType,
            @RequestHeader(name = "X-Correlation-Cust-Id", required = false) String correlationId,
            @Valid @RequestBody CreateTravelcardRequestDto request) {
        CreateTravelcardResponseDto response = travelcardService.createTravelcard(request);
        return ResponseEntity.ok(response);
    }
}
