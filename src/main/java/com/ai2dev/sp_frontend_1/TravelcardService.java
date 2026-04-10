package com.ai2dev.sp_frontend_1;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.core.env.Environment;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import org.springframework.web.client.HttpClientErrorException;
import org.springframework.web.client.HttpServerErrorException;
import org.springframework.web.client.RestClientException;
import org.springframework.web.client.RestTemplate;

/**
 * Business logic for forwarding Travelcard requests to the protected API.
 */
@Service
public class TravelcardService {
    private static final Logger logger = LoggerFactory.getLogger(TravelcardService.class);

    private final RestTemplate restTemplate;
    private final Environment env;

    public TravelcardService(SbConnectionClient client, Environment env) {
        this.restTemplate = client.restTemplate();
        this.env = env;
    }

    public ResponseEntity<String> forwardRequest(String body, org.springframework.http.HttpHeaders incomingHeaders) {
        logger.info("Enter TravelcardService.forwardRequest");

        String baseUrl = env.getProperty("TRAVELCARD_API_URL");
        String functionKey = env.getProperty("TRAVELCARD_FUNCTION_KEY");

        if (baseUrl == null || baseUrl.isEmpty()) {
            logger.error("Environment variable TRAVELCARD_API_URL is not set");
            throw new ExternalApiException(HttpStatus.INTERNAL_SERVER_ERROR, "TRAVELCARD_API_URL not configured");
        }
        if (functionKey == null || functionKey.isEmpty()) {
            logger.error("Environment variable TRAVELCARD_FUNCTION_KEY is not set");
            throw new ExternalApiException(HttpStatus.INTERNAL_SERVER_ERROR, "TRAVELCARD_FUNCTION_KEY not configured");
        }

        String url = baseUrl + "?code=" + functionKey;

        HttpHeaders headers = new HttpHeaders();
        headers.setContentType(MediaType.APPLICATION_JSON);

        // client_id header: attempt to read from AZURE-CLIENT-ID
        String clientId = env.getProperty("AZURE-CLIENT-ID");
        if (clientId != null && !clientId.isEmpty()) {
            headers.set("client_id", clientId);
        }

        // Authorization header is attached by sb-connection RestTemplate interceptor

        HttpEntity<String> entity = new HttpEntity<>(body, headers);

        try {
            ResponseEntity<String> response = restTemplate.postForEntity(url, entity, String.class);
            logger.info("Exit TravelcardService.forwardRequest with status {}", response.getStatusCode());
            return ResponseEntity.status(response.getStatusCode()).body(response.getBody());
        } catch (HttpClientErrorException | HttpServerErrorException e) {
            logger.error("External API returned error: {}", e.getStatusCode(), e);
            HttpStatus status;
            try {
                status = HttpStatus.valueOf(e.getStatusCode().value());
            } catch (Exception ex) {
                status = HttpStatus.BAD_GATEWAY;
            }
            throw new ExternalApiException(status, e.getResponseBodyAsString());
        } catch (RestClientException e) {
            logger.error("Connection failure to external API", e);
            throw new ExternalApiException(HttpStatus.BAD_GATEWAY, "Failed to connect to external Travelcard API");
        }
    }
}
