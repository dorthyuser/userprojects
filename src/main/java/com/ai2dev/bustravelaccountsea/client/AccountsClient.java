package com.ai2dev.bustravelaccountsea.client;

import com.ai2dev.bustravelaccountsea.config.Settings;
import com.ai2dev.bustravelaccountsea.exception.MuleBusinessException;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Component;
import org.springframework.web.client.RestClient;

@Component
public class AccountsClient {
    private static final Logger log = LoggerFactory.getLogger(AccountsClient.class);
    private final RestClient restClient;
    private final Settings settings;

    public AccountsClient(@Qualifier("accountsRestClient") RestClient restClient, Settings settings) {
        this.restClient = restClient;
        this.settings = settings;
    }

    public ResponseEntity<?> getAccounts(String customerCorrelationId, String correlationId, String clientId, String email) {
        try {
            ResponseEntity<String> response = restClient.get()
                    .uri(uriBuilder -> uriBuilder.path("/accounts").queryParam("email", email).build())
                    .header("CUSTOMER_CORRELATION_ID", customerCorrelationId)
                    .header("X_CORRELATION_ID", correlationId)
                    .header("client_secret", settings.httpsRequesterSaAccountsClientSecret())
                    .header("client_id", clientId == null ? settings.httpsRequesterSaAccountsClientId() : clientId)
                    .retrieve()
                    .toEntity(String.class);
            log.info("AccountsClient GET /accounts status {}", response.getStatusCode().value());
            return ResponseEntity.status(response.getStatusCode()).headers(response.getHeaders()).body(response.getBody());
        } catch (Exception e) {
            log.error("AccountsClient GET /accounts failed", e);
            throw new MuleBusinessException("MULE UNKNOWN ERROR", "An unexpected error occurred", e);
        }
    }

    public ResponseEntity<?> getAccount(String id, String customerCorrelationId, String correlationId, String clientId) {
        try {
            ResponseEntity<String> response = restClient.get()
                    .uri(uriBuilder -> uriBuilder.path("/accounts/{id}").build(id))
                    .header("CUSTOMER_CORRELATION_ID", customerCorrelationId)
                    .header("X_CORRELATION_ID", correlationId)
                    .header("client_secret", settings.httpsRequesterSaAccountsClientSecret())
                    .header("client_id", clientId == null ? settings.httpsRequesterSaAccountsClientId() : clientId)
                    .retrieve()
                    .toEntity(String.class);
            log.info("AccountsClient GET /accounts/{} status {}", id, response.getStatusCode().value());
            return ResponseEntity.status(response.getStatusCode()).headers(response.getHeaders()).body(response.getBody());
        } catch (Exception e) {
            log.error("AccountsClient GET /accounts/{id} failed", e);
            throw new MuleBusinessException("MULE UNKNOWN ERROR", "An unexpected error occurred", e);
        }
    }

    public ResponseEntity<?> postAccounts(String body, String customerCorrelationId, String correlationId, String clientId) {
        try {
            ResponseEntity<String> response = restClient.post()
                    .uri("/accounts")
                    .header("CUSTOMER_CORRELATION_ID", customerCorrelationId)
                    .header("X_CORRELATION_ID", correlationId)
                    .header("client_secret", settings.httpsRequesterSaAccountsClientSecret())
                    .header("client_id", clientId == null ? settings.httpsRequesterSaAccountsClientId() : clientId)
                    .contentType(MediaType.APPLICATION_JSON)
                    .body(body)
                    .retrieve()
                    .toEntity(String.class);
            log.info("AccountsClient POST /accounts status {}", response.getStatusCode().value());
            return ResponseEntity.status(response.getStatusCode()).headers(response.getHeaders()).body(response.getBody());
        } catch (Exception e) {
            log.error("AccountsClient POST /accounts failed", e);
            throw new MuleBusinessException("MULE UNKNOWN ERROR", "An unexpected error occurred", e);
        }
    }

    public ResponseEntity<?> putAccounts(String id, String body, String customerCorrelationId, String correlationId, String clientId) {
        try {
            ResponseEntity<String> response = restClient.put()
                    .uri(uriBuilder -> uriBuilder.path("/accounts/{id}").build(id))
                    .header("CUSTOMER_CORRELATION_ID", customerCorrelationId)
                    .header("X_CORRELATION_ID", correlationId)
                    .header("client_secret", settings.httpsRequesterSaAccountsClientSecret())
                    .header("client_id", clientId == null ? settings.httpsRequesterSaAccountsClientId() : clientId)
                    .contentType(MediaType.APPLICATION_JSON)
                    .body(body)
                    .retrieve()
                    .toEntity(String.class);
            log.info("AccountsClient PUT /accounts/{} status {}", id, response.getStatusCode().value());
            return ResponseEntity.status(response.getStatusCode()).headers(response.getHeaders()).body(response.getBody());
        } catch (Exception e) {
            log.error("AccountsClient PUT /accounts/{id} failed", e);
            throw new MuleBusinessException("MULE UNKNOWN ERROR", "An unexpected error occurred", e);
        }
    }
}
