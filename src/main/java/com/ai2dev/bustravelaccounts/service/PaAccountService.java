package com.ai2dev.bustravelaccounts.service;

import com.ai2dev.bustravelaccounts.model.response.PaApiResult;
import com.ai2dev.bustravelaccounts.model.response.SaApiResult;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import tools.jackson.databind.JsonNode;
import tools.jackson.databind.ObjectMapper;
import org.springframework.stereotype.Service;

import java.util.HashMap;
import java.util.Map;

@Service
public class PaAccountService {
    private static final Logger log = LoggerFactory.getLogger(PaAccountService.class);
    private final SaApiService saApiService;
    private final ObjectMapper mapper;

    public PaAccountService(SaApiService saApiService, ObjectMapper mapper) {
        this.saApiService = saApiService;
        this.mapper = mapper;
    }

    public PaApiResult getAccounts(Map<String, String> query, Map<String, String> headers) {
        try {
            SaApiResult result = saApiService.getAccounts(null, query, headers);
            return copy(result);
        } catch (Exception e) {
            log.error("GET /accounts failed", e);
            return error();
        }
    }

    public PaApiResult getAccountById(String id, Map<String, String> headers) {
        try {
            SaApiResult result = saApiService.getAccountById(id, Map.of(), headers);
            return copy(result);
        } catch (Exception e) {
            log.error("GET /accounts/{id} failed", e);
            return error();
        }
    }

    public PaApiResult postAccounts(String body, Map<String, String> headers) {
        try {
            JsonNode node = mapper.readTree(body);
            String personEmail = node.path("personEmail").asText("").trim().toLowerCase();
            Map<String, String> query = new HashMap<>();
            query.put("email", personEmail);
            SaApiResult existing = saApiService.getAccounts(null, query, headers);
            if (existing.statusCode() == 200) {
                try {
                    if (mapper.readTree(existing.body()).isArray() && mapper.readTree(existing.body()).size() > 0) {
                        return new PaApiResult(409, mapper.writeValueAsString(Map.of("error", Map.of("errorCode", 409, "errorDateTime", java.time.OffsetDateTime.now().toString(), "errorMessage", "CONFLICT", "errorDescription", "An account with this email already exists"))), Map.of("Content-Type", "application/json"));
                    }
                } catch (Exception ignored) {
                }
            }
            if (!(existing.statusCode() == 200 || existing.statusCode() == 204)) {
                return copy(existing);
            }
            SaApiResult created = saApiService.postAccounts(body, Map.of(), headers);
            return copy(created);
        } catch (Exception e) {
            log.error("POST /accounts failed", e);
            return error();
        }
    }

    public PaApiResult putAccounts(String id, String body, Map<String, String> headers) {
        try {
            SaApiResult existing = saApiService.getAccountById(id, Map.of(), headers);
            if (existing.statusCode() == 204) {
                return new PaApiResult(404, mapper.writeValueAsString(Map.of("error", Map.of("errorCode", 404, "errorDateTime", java.time.OffsetDateTime.now().toString(), "errorMessage", "RESOURCE NOT FOUND", "errorDescription", "Account not found"))), Map.of("Content-Type", "application/json"));
            }
            if (existing.statusCode() == 200) {
                try {
                    JsonNode node = mapper.readTree(existing.body());
                    if (node.path("hotlisted").asBoolean(false)) {
                        return new PaApiResult(422, mapper.writeValueAsString(Map.of("error", Map.of("errorCode", 422, "errorDateTime", java.time.OffsetDateTime.now().toString(), "errorMessage", "UNPROCESSABLE ENTITY", "errorDescription", "Account is hotlisted and cannot be changed"))), Map.of("Content-Type", "application/json"));
                    }
                } catch (Exception ignored) {
                }
            }
            if (!(existing.statusCode() == 200 || existing.statusCode() == 204)) {
                return copy(existing);
            }
            SaApiResult updated = saApiService.putAccounts(id, body, Map.of(), headers);
            return copy(updated);
        } catch (Exception e) {
            log.error("PUT /accounts/{id} failed", e);
            return error();
        }
    }

    public PaApiResult alive() {
        return new PaApiResult(200, "UP", Map.of("Content-Type", "text/plain"));
    }

    public PaApiResult ready() {
        return new PaApiResult(200, "UP", Map.of("Content-Type", "text/plain"));
    }

    private PaApiResult copy(SaApiResult result) {
        return new PaApiResult(result.statusCode(), result.body(), result.headers());
    }

    private PaApiResult error() {
        return new PaApiResult(500, "{\"error\":{\"errorCode\":500,\"errorDateTime\":\"\",\"errorMessage\":\"MULE UNKNOWN ERROR\",\"errorDescription\":\"An unexpected error occurred\"}}", Map.of("Content-Type", "application/json"));
    }
}
