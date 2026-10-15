package com.ai2dev.bustravelaccounts.service;

import com.ai2dev.bustravelaccounts.model.response.SaApiResult;

import com.ai2dev.bustravelaccounts.model.response.PaApiResult;
import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

@Service
public class PaService {
    private static final Logger log = LoggerFactory.getLogger(PaService.class);
    private final SaApiService saApiService;

    public PaService(SaApiService saApiService) {
        this.saApiService = saApiService;
    }

    public PaApiResult getAccounts(String email, Map<String, String> query, Map<String, String> headers) {
        try {
            SaApiResult result = saApiService.getAccounts(email, query, headers);
            return new PaApiResult(result.statusCode(), result.body(), result.headers());
        } catch (Exception e) {
            log.error("GET /accounts failed", e);
            return error500();
        }
    }

    public PaApiResult getAccountById(String id, Map<String, String> query, Map<String, String> headers) {
        try {
            SaApiResult result = saApiService.getAccountById(id, query, headers);
            if (result.statusCode() == 204) {
                return new PaApiResult(404, "{\"error\":{\"errorCode\":404,\"errorDateTime\":\"\",\"errorMessage\":\"RESOURCE NOT FOUND\",\"errorDescription\":\"Account not found\"}}", Map.of("Content-Type", "application/json"));
            }
            return new PaApiResult(result.statusCode(), result.body(), result.headers());
        } catch (Exception e) {
            log.error("GET /accounts/{id} failed", e);
            return error500();
        }
    }

    public PaApiResult postAccounts(String body, Map<String, String> query, Map<String, String> headers) {
        try {
            SaApiResult existing = saApiService.getAccounts(extractEmail(body), query, headers);
            if (existing.statusCode() == 200 && existing.body() != null && !existing.body().equals("[]")) {
                return new PaApiResult(409, "{\"error\":{\"errorCode\":409,\"errorDateTime\":\"\",\"errorMessage\":\"CONFLICT\",\"errorDescription\":\"An account with this email already exists\"}}", Map.of("Content-Type", "application/json"));
            }
            if (existing.statusCode() != 200 && existing.statusCode() != 204) {
                return new PaApiResult(existing.statusCode(), existing.body(), existing.headers());
            }
            SaApiResult result = saApiService.postAccounts(body, query, headers);
            return new PaApiResult(result.statusCode(), result.body(), result.headers());
        } catch (Exception e) {
            log.error("POST /accounts failed", e);
            return error500();
        }
    }

    public PaApiResult putAccounts(String id, String body, Map<String, String> query, Map<String, String> headers) {
        try {
            SaApiResult existing = saApiService.getAccountById(id, query, headers);
            if (existing.statusCode() == 204) {
                return new PaApiResult(404, "{\"error\":{\"errorCode\":404,\"errorDateTime\":\"\",\"errorMessage\":\"RESOURCE NOT FOUND\",\"errorDescription\":\"Account not found\"}}", Map.of("Content-Type", "application/json"));
            }
            if (existing.statusCode() == 200 && existing.body() != null && existing.body().contains("\"hotlisted\":true")) {
                return new PaApiResult(422, "{\"error\":{\"errorCode\":422,\"errorDateTime\":\"\",\"errorMessage\":\"UNPROCESSABLE ENTITY\",\"errorDescription\":\"Account is hotlisted and cannot be changed\"}}", Map.of("Content-Type", "application/json"));
            }
            if (existing.statusCode() != 200) {
                return new PaApiResult(existing.statusCode(), existing.body(), existing.headers());
            }
            SaApiResult result = saApiService.putAccounts(id, body, query, headers);
            return new PaApiResult(result.statusCode(), result.body(), result.headers());
        } catch (Exception e) {
            log.error("PUT /accounts/{id} failed", e);
            return error500();
        }
    }

    private String extractEmail(String body) {
        try {
            tools.jackson.databind.JsonNode node = tools.jackson.databind.json.JsonMapper.builder().build().readTree(body);
            tools.jackson.databind.JsonNode email = node.get("personEmail");
            return email == null || email.isNull() ? null : email.asString().trim().toLowerCase();
        } catch (Exception e) {
            return null;
        }
    }

    private PaApiResult error500() {
        return new PaApiResult(500, "{\"error\":{\"errorCode\":500,\"errorDateTime\":\"\",\"errorMessage\":\"MULE UNKNOWN ERROR\",\"errorDescription\":\"An unexpected error occurred\"}}", Map.of("Content-Type", "application/json"));
    }
}
