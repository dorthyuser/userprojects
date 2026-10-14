package com.ai2dev.bustravelaccounts.service;

import com.ai2dev.bustravelaccounts.model.response.SaApiResult;
import org.springframework.stereotype.Service;

import java.util.Map;

@Service
public class SaApiService {
    private final SaAccountService accountService;

    public SaApiService(SaAccountService accountService) {
        this.accountService = accountService;
    }

    /** GET /accounts */
    public SaApiResult getAccounts(String id, Map<String, String> query, Map<String, String> headers) {
        return accountService.getAccounts(query, headers);
    }

    /** GET /accounts/{id} */
    public SaApiResult getAccountById(String id, Map<String, String> query, Map<String, String> headers) {
        return accountService.getAccountById(id, headers);
    }

    /** POST /accounts */
    public SaApiResult postAccounts(String body, Map<String, String> query, Map<String, String> headers) {
        return accountService.postAccounts(body, headers);
    }

    /** PUT /accounts/{id} */
    public SaApiResult putAccounts(String id, String body, Map<String, String> query, Map<String, String> headers) {
        return accountService.putAccounts(id, body, headers);
    }

    /** ANY /alive */
    public SaApiResult alive() {
        return accountService.alive();
    }

    /** ANY /ready */
    public SaApiResult ready() {
        return accountService.ready();
    }
}
