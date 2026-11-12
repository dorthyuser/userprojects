package com.ai2dev.bustravelaccountsea.service;

import com.ai2dev.bustravelaccountsea.client.SystemAccountsClient;
import com.ai2dev.bustravelaccountsea.config.Settings;
import com.ai2dev.bustravelaccountsea.dto.response.AccountsResponse;
import com.ai2dev.bustravelaccountsea.util.CorrelationIdUtil;
import com.ai2dev.bustravelaccountsea.util.HeaderUtil;
import jakarta.servlet.http.HttpServletRequest;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpHeaders;
import org.springframework.stereotype.Service;

@Service
public class AccountsService {
    private static final Logger log = LoggerFactory.getLogger(AccountsService.class);
    private final SystemAccountsClient client;
    private final Settings settings;

    public AccountsService(SystemAccountsClient client, Settings settings) {
        this.client = client;
        this.settings = settings;
    }

    public AccountsResponse getAccounts(String email, HttpHeaders headers, HttpServletRequest request) {
        String correlationId = CorrelationIdUtil.correlationId(headers);
        String customerCorrelationId = HeaderUtil.header(headers, "CUSTOMER_CORRELATION_ID");
        String clientId = settings.httpsRequesterSaAccountsClientId();
        String clientSecret = settings.httpsRequesterSaAccountsClientSecret();
        String emailId = email == null ? null : email.trim();
        log.debug("Before Request - bus-travel-accounts-sa-get-accounts");
        return client.getAccounts(customerCorrelationId, correlationId, clientSecret, clientId, emailId);
    }

    public AccountsResponse getAccount(String id, HttpHeaders headers, HttpServletRequest request) {
        String correlationId = CorrelationIdUtil.correlationId(headers);
        String customerCorrelationId = HeaderUtil.header(headers, "CUSTOMER_CORRELATION_ID");
        String clientId = settings.httpsRequesterSaAccountsClientId();
        String clientSecret = settings.httpsRequesterSaAccountsClientSecret();
        String accountId = id == null ? null : id.trim();
        log.debug("Before Request - bus-travel-accounts-sa-get-account-details");
        return client.getAccount(customerCorrelationId, correlationId, clientSecret, clientId, accountId);
    }

    public AccountsResponse postAccounts(String body, HttpHeaders headers, HttpServletRequest request) {
        String correlationId = CorrelationIdUtil.correlationId(headers);
        String customerCorrelationId = HeaderUtil.header(headers, "CUSTOMER_CORRELATION_ID");
        String clientId = settings.httpsRequesterSaAccountsClientId();
        String clientSecret = settings.httpsRequesterSaAccountsClientSecret();
        log.debug("Before Request - bus-travel-accounts-sa-create-Accounts");
        return client.postAccounts(customerCorrelationId, correlationId, clientSecret, clientId, body);
    }

    public AccountsResponse putAccounts(String id, String body, HttpHeaders headers, HttpServletRequest request) {
        String correlationId = CorrelationIdUtil.correlationId(headers);
        String customerCorrelationId = HeaderUtil.header(headers, "CUSTOMER_CORRELATION_ID");
        String clientId = settings.httpsRequesterSaAccountsClientId();
        String clientSecret = settings.httpsRequesterSaAccountsClientSecret();
        String accountId = id == null ? null : id.trim();
        log.debug("Before Request - bus-travel-accounts-sa-update-account");
        return client.putAccounts(customerCorrelationId, correlationId, clientSecret, clientId, accountId, body);
    }
}
