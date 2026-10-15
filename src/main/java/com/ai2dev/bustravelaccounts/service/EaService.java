package com.ai2dev.bustravelaccounts.service;

import com.ai2dev.bustravelaccounts.model.response.PaApiResult;
import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

@Service
public class EaService {
    private static final Logger log = LoggerFactory.getLogger(EaService.class);
    private final PaApiService paApiService;

    public EaService(PaApiService paApiService) {
        this.paApiService = paApiService;
    }

    public PaApiResult getAccounts(String email, Map<String, String> query, Map<String, String> headers) {
        try {
            PaApiResult result = paApiService.getAccounts(email, query, headers);
            return new PaApiResult(result.statusCode(), result.body(), result.headers());
        } catch (Exception e) {
            log.error("GET /accounts failed", e);
            return error500();
        }
    }

    public PaApiResult getAccountById(String id, Map<String, String> query, Map<String, String> headers) {
        try {
            PaApiResult result = paApiService.getAccountById(id, query, headers);
            return new PaApiResult(result.statusCode(), result.body(), result.headers());
        } catch (Exception e) {
            log.error("GET /accounts/{id} failed", e);
            return error500();
        }
    }

    public PaApiResult postAccounts(String body, Map<String, String> query, Map<String, String> headers) {
        try {
            PaApiResult result = paApiService.postAccounts(body, query, headers);
            return new PaApiResult(result.statusCode(), result.body(), result.headers());
        } catch (Exception e) {
            log.error("POST /accounts failed", e);
            return error500();
        }
    }

    public PaApiResult putAccounts(String id, String body, Map<String, String> query, Map<String, String> headers) {
        try {
            PaApiResult result = paApiService.putAccounts(id, body, query, headers);
            return new PaApiResult(result.statusCode(), result.body(), result.headers());
        } catch (Exception e) {
            log.error("PUT /accounts/{id} failed", e);
            return error500();
        }
    }

    private PaApiResult error500() {
        return new PaApiResult(500, "{\"error\":{\"errorCode\":500,\"errorDateTime\":\"\",\"errorMessage\":\"MULE UNKNOWN ERROR\",\"errorDescription\":\"An unexpected error occurred\"}}", Map.of("Content-Type", "application/json"));
    }
}
