package com.ai2dev.travelcardsplambda425.controller;

import com.ai2dev.travelcardsplambda425.model.CreateTravelcardRequest;
import com.ai2dev.travelcardsplambda425.model.CreateTravelcardResponse;
import com.ai2dev.travelcardsplambda425.service.TravelcardService;
import jakarta.validation.Valid;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.MediaType;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestBody;
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
    public CreateTravelcardResponse create(
            @RequestHeader("client_id") String clientId,
            @RequestHeader(value = "X-Correlation-Cust-Id", required = false) String correlationId,
            @Valid @RequestBody CreateTravelcardRequest request) {
        log.info("Entering TravelcardController.create clientId={} correlationId={}", clientId, correlationId);
        CreateTravelcardResponse response = travelcardService.create(request);
        log.info("Exiting TravelcardController.create");
        return response;
    }
}