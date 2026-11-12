package com.ai2dev.bustravelaccountsea.service;

import com.ai2dev.bustravelaccountsea.client.AccountsClient;
import com.ai2dev.bustravelaccountsea.util.CorrelationIdUtil;
import com.ai2dev.bustravelaccountsea.util.HeaderUtil;
import org.springframework.http.HttpHeaders;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;

@Service
public class AccountsService {
    private final AccountsClient client;

    public AccountsService(AccountsClient client) {
        this.client = client;
    }

    public ResponseEntity<?> getAccounts(HttpHeaders headers, String queryString) {
        String customerCorrelationId = HeaderUtil.trimOrDefault(headers.getFirst("CUSTOMER_CORRELATION_ID"), "CUSTOMER_CORRELATION_ID_NOT_FOUND");
        String correlationId = CorrelationIdUtil.resolve(headers);
        String clientId = HeaderUtil.trimOrNull(headers.getFirst("client_id"));
        String email = HeaderUtil.extractQueryParam(queryString, "email");
        return client.getAccounts(customerCorrelationId, correlationId, clientId, email);
    }

    public ResponseEntity<?> getAccount(String id, HttpHeaders headers) {
        String customerCorrelationId = HeaderUtil.trimOrDefault(headers.getFirst("CUSTOMER_CORRELATION_ID"), "CUSTOMER_CORRELATION_ID_NOT_FOUND");
        String correlationId = CorrelationIdUtil.resolve(headers);
        String clientId = HeaderUtil.trimOrNull(headers.getFirst("client_id"));
        return client.getAccount(HeaderUtil.trimOrNull(id), customerCorrelationId, correlationId, clientId);
    }

    public ResponseEntity<?> postAccounts(String body, HttpHeaders headers) {
        String customerCorrelationId = HeaderUtil.trimOrDefault(headers.getFirst("CUSTOMER_CORRELATION_ID"), "CUSTOMER_CORRELATION_ID_NOT_FOUND");
        String correlationId = CorrelationIdUtil.resolve(headers);
        String clientId = HeaderUtil.trimOrNull(headers.getFirst("client_id"));
        return client.postAccounts(body, customerCorrelationId, correlationId, clientId);
    }

    public ResponseEntity<?> putAccounts(String id, String body, HttpHeaders headers) {
        String customerCorrelationId = HeaderUtil.trimOrDefault(headers.getFirst("CUSTOMER_CORRELATION_ID"), "CUSTOMER_CORRELATION_ID_NOT_FOUND");
        String correlationId = CorrelationIdUtil.resolve(headers);
        String clientId = HeaderUtil.trimOrNull(headers.getFirst("client_id"));
        return client.putAccounts(HeaderUtil.trimOrNull(id), body, customerCorrelationId, correlationId, clientId);
    }
}
