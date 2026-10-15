package com.ai2dev.bustravelaccounts.service;

import com.ai2dev.bustravelaccounts.model.response.SaApiResult;
import java.util.Map;
import org.springframework.stereotype.Service;

@Service
public class SaApiService {
    private final SaService service;

    public SaApiService(SaService service) {
        this.service = service;
    }

    /** GET /accounts */
    public SaApiResult getAccounts(String email, Map<String, String> query, Map<String, String> headers) {
        return service.getAccounts(email, query, headers);
    }

    /** GET /accounts/{id} */
    public SaApiResult getAccountById(String id, Map<String, String> query, Map<String, String> headers) {
        return service.getAccountById(id, query, headers);
    }

    /** POST /accounts */
    public SaApiResult postAccounts(String body, Map<String, String> query, Map<String, String> headers) {
        return service.postAccounts(body, query, headers);
    }

    /** PUT /accounts/{id} */
    public SaApiResult putAccounts(String id, String body, Map<String, String> query, Map<String, String> headers) {
        return service.putAccounts(id, body, query, headers);
    }
}
