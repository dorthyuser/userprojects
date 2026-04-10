package com.ai2dev.sp_frontend_1;

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

/**
 * Controller that accepts Travelcard requests and forwards them to the protected Travelcard API.
 */
@RestController
@RequestMapping("/")
@Validated
public class TravelcardController {
    private static final Logger logger = LoggerFactory.getLogger(TravelcardController.class);

    private final TravelcardService service;

    public TravelcardController(TravelcardService service) {
        this.service = service;
    }

    @PostMapping(path = "/travelcard", consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<String> forwardTravelcard(@RequestBody String body, @RequestHeader HttpHeaders incomingHeaders) {
        logger.info("Enter TravelcardController.forwardTravelcard");

        try {
            ResponseEntity<String> resp = service.forwardRequest(body, incomingHeaders);
            logger.info("Exit TravelcardController.forwardTravelcard with status {}", resp.getStatusCode());
            return resp;
        } catch (TokenException e) {
            logger.error("Token generation failed", e);
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body("Token generation failed: " + e.getMessage());
        } catch (ExternalApiException e) {
            logger.error("External API error", e);
            return ResponseEntity.status(e.getStatus()).body(e.getBody() != null ? e.getBody() : ("External API error: " + e.getMessage()));
        } catch (Exception e) {
            logger.error("Unexpected error", e);
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body("Unexpected server error");
        }
    }
}
