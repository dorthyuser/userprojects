package com.ai2dev.bustravelaccountsea.client;

import com.ai2dev.bustravelaccountsea.dto.response.AccountsResponse;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Component;
import org.springframework.web.client.RestClient;

@Component
public class SystemAccountsClient {
    private static final Logger log = LoggerFactory.getLogger(SystemAccountsClient.class);
    private final RestClient restClient;

    public SystemAccountsClient(RestClient systemAccountsRestClient) {
        this.restClient = systemAccountsRestClient;
    }

    public AccountsResponse getAccounts(String customerCorrelationId, String correlationId, String clientSecret, String clientId, String email) {
        ResponseEntity<String> response = restClient.get()
                .uri(uriBuilder -> uriBuilder.path("/accounts").queryParam("email", email).build())
                .header("CUSTOMER_CORRELATION_ID", customerCorrelationId)
                .header("X_CORRELATION_ID", correlationId)
                .header("client_secret", clientSecret)
                .header("client_id", clientId)
                .accept(MediaType.APPLICATION_JSON)
                .exchange((request, clientResponse) -> ResponseEntity.status(clientResponse.getStatusCode()).headers(clientResponse.getHeaders()).body(clientResponse.bodyTo(String.class)));
        log.info("SystemAccountsClient GET /accounts status {}", response.getStatusCode().value());
        return new AccountsResponse(response.getStatusCode().value(), response.getHeaders(), response.getBody());
    }

    public AccountsResponse getAccount(String customerCorrelationId, String correlationId, String clientSecret, String clientId, String id) {
        ResponseEntity<String> response = restClient.get()
                .uri(uriBuilder -> uriBuilder.path("/accounts/{id}").build(id))
                .header("CUSTOMER_CORRELATION_ID", customerCorrelationId)
                .header("X_CORRELATION_ID", correlationId)
                .header("client_secret", clientSecret)
                .header("client_id", clientId)
                .accept(MediaType.APPLICATION_JSON)
                .exchange((request, clientResponse) -> ResponseEntity.status(clientResponse.getStatusCode()).headers(clientResponse.getHeaders()).body(clientResponse.bodyTo(String.class)));
        log.info("SystemAccountsClient GET /accounts/{id} status {}", response.getStatusCode().value());
        return new AccountsResponse(response.getStatusCode().value(), response.getHeaders(), response.getBody());
    }

    public AccountsResponse postAccounts(String customerCorrelationId, String correlationId, String clientSecret, String clientId, String body) {
        ResponseEntity<String> response = restClient.post()
                .uri("/accounts")
                .header("CUSTOMER_CORRELATION_ID", customerCorrelationId)
                .header("X_CORRELATION_ID", correlationId)
                .header("client_secret", clientSecret)
                .header("client_id", clientId)
                .contentType(MediaType.APPLICATION_JSON)
                .body(body)
                .exchange((request, clientResponse) -> ResponseEntity.status(clientResponse.getStatusCode()).headers(clientResponse.getHeaders()).body(clientResponse.bodyTo(String.class)));
        log.info("SystemAccountsClient POST /accounts status {}", response.getStatusCode().value());
        return new AccountsResponse(response.getStatusCode().value(), response.getHeaders(), response.getBody());
    }

    public AccountsResponse putAccounts(String customerCorrelationId, String correlationId, String clientSecret, String clientId, String id, String body) {
        ResponseEntity<String> response = restClient.put()
                .uri(uriBuilder -> uriBuilder.path("/accounts/{id}").build(id))
                .header("CUSTOMER_CORRELATION_ID", customerCorrelationId)
                .header("X_CORRELATION_ID", correlationId)
                .header("client_secret", clientSecret)
                .header("client_id", clientId)
                .contentType(MediaType.APPLICATION_JSON)
                .body(body)
                .exchange((request, clientResponse) -> ResponseEntity.status(clientResponse.getStatusCode()).headers(clientResponse.getHeaders()).body(clientResponse.bodyTo(String.class)));
        log.info("SystemAccountsClient PUT /accounts/{id} status {}", response.getStatusCode().value());
        return new AccountsResponse(response.getStatusCode().value(), response.getHeaders(), response.getBody());
    }
}
