package com.ai2dev.bustravelaccounts.service;

import com.ai2dev.bustravelaccounts.client.SaDatabaseClient;
import com.ai2dev.bustravelaccounts.model.response.SaApiResult;
import java.util.Map;
import org.springframework.stereotype.Service;

@Service
public class SaService {
    private final SaDatabaseClient databaseClient;

    public SaService(SaDatabaseClient databaseClient) {
        this.databaseClient = databaseClient;
    }

    public SaApiResult getAccounts(String email, Map<String, String> query, Map<String, String> headers) {
        return databaseClient.getAccounts(email);
    }

    public SaApiResult getAccountById(String id, Map<String, String> query, Map<String, String> headers) {
        return databaseClient.getAccountById(id);
    }

    public SaApiResult postAccounts(String body, Map<String, String> query, Map<String, String> headers) {
        return databaseClient.postAccounts(body);
    }

    public SaApiResult putAccounts(String id, String body, Map<String, String> query, Map<String, String> headers) {
        return databaseClient.putAccounts(id, body);
    }
}
