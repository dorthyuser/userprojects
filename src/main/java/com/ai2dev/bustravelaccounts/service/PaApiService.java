package com.ai2dev.bustravelaccounts.service;

import com.ai2dev.bustravelaccounts.model.response.PaApiResult;
import org.springframework.stereotype.Service;

import java.util.Map;

@Service
public class PaApiService {
    private final PaAccountService accountService;

    public PaApiService(PaAccountService accountService) {
        this.accountService = accountService;
    }

    /** GET /accounts */
    public PaApiResult getAccounts(String id, Map<String, String> query, Map<String, String> headers) {
        return accountService.getAccounts(query, headers);
    }

    /** GET /accounts/{id} */
    public PaApiResult getAccountById(String id, Map<String, String> query, Map<String, String> headers) {
        return accountService.getAccountById(id, headers);
    }

    /** POST /accounts */
    public PaApiResult postAccounts(String body, Map<String, String> query, Map<String, String> headers) {
        return accountService.postAccounts(body, headers);
    }

    /** PUT /accounts/{id} */
    public PaApiResult putAccounts(String id, String body, Map<String, String> query, Map<String, String> headers) {
        return accountService.putAccounts(id, body, headers);
    }

    /** ANY /alive */
    public PaApiResult alive() {
        return accountService.alive();
    }

    /** ANY /ready */
    public PaApiResult ready() {
        return accountService.ready();
    }
}
