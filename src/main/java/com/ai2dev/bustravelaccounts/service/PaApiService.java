package com.ai2dev.bustravelaccounts.service;

import com.ai2dev.bustravelaccounts.model.response.PaApiResult;
import java.util.Map;
import org.springframework.stereotype.Service;

@Service
public class PaApiService {
    private final PaService service;

    public PaApiService(PaService service) {
        this.service = service;
    }

    /** GET /accounts */
    public PaApiResult getAccounts(String email, Map<String, String> query, Map<String, String> headers) {
        return service.getAccounts(email, query, headers);
    }

    /** GET /accounts/{id} */
    public PaApiResult getAccountById(String id, Map<String, String> query, Map<String, String> headers) {
        return service.getAccountById(id, query, headers);
    }

    /** POST /accounts */
    public PaApiResult postAccounts(String body, Map<String, String> query, Map<String, String> headers) {
        return service.postAccounts(body, query, headers);
    }

    /** PUT /accounts/{id} */
    public PaApiResult putAccounts(String id, String body, Map<String, String> query, Map<String, String> headers) {
        return service.putAccounts(id, body, query, headers);
    }
}
